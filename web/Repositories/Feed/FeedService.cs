using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Feed.Dtos;
using web.Repositories.Feed.Interfaces;
using web.Services.AiGateway.Interfaces;

namespace web.Repositories.Feed
{
    public class FeedService : IFeedService
    {
        private record AuthorInfo(string DisplayName, bool HasAvatar);

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<FeedService> _logger;
        private readonly LanguageTools _language;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;

        public FeedService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<FeedService> logger,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
            _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
        }

        // Overrides DefaultChatModel with AiGateway:TranslationModel when one is configured — see
        // AiGatewaySettings.TranslationModel for why: DefaultChatModel is sometimes a reasoning model,
        // which turned out (confirmed by live testing, originally against the Documents "Oversæt"-button)
        // to be unreliable and slow for translation, silently spending its whole token budget on hidden
        // reasoning instead of ever answering. Resolved once per call site rather than cached on the
        // instance, since FeedService is scoped per-request and the setting can change between requests.
        private async Task<string?> ResolveTranslationModelAsync(CancellationToken ct)
        {
            var aiConfig = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(ct);
            return string.IsNullOrWhiteSpace(aiConfig.TranslationModel) ? null : aiConfig.TranslationModel;
        }

        public async Task<GetFeedPageResponseDto> GetFeedPageAsync(GetFeedPageRequestDto dto, CancellationToken ct = default)
        {
            var take = Math.Clamp(dto.Take <= 0 ? 10 : dto.Take, 1, 30);
            var viewerLanguage = AppLanguages.Normalize(dto.ViewerLanguage);

            var query = _context.FeedPosts
                .AsNoTracking()
                .AsQueryable();

            if (dto.BeforeId.HasValue)
            {
                // The timeline sorts by CreatedAtUtc (Administrator/Developer can backdate a post, e.g.
                // to add an old event, and it must then sort into its real place, not stay pinned to
                // the top by insertion order) with Id as a tiebreaker for posts sharing a timestamp.
                // Paging "before" that ordering therefore needs the anchor post's own CreatedAtUtc, not
                // just its Id.
                var anchor = await _context.FeedPosts
                    .AsNoTracking()
                    .Where(p => p.Id == dto.BeforeId.Value)
                    .Select(p => new { p.CreatedAtUtc, p.Id })
                    .FirstOrDefaultAsync(ct);

                // The anchor post was deleted since the caller last saw it — there's no reliable way to
                // tell what came "after" it any more, so stop paging rather than risk re-showing posts
                // already rendered on the client.
                if (anchor is null)
                    return new GetFeedPageResponseDto { Posts = new List<FeedPostDto>(), HasMore = false };

                query = query.Where(p =>
                    p.CreatedAtUtc < anchor.CreatedAtUtc ||
                    (p.CreatedAtUtc == anchor.CreatedAtUtc && p.Id < anchor.Id));
            }

            var posts = await query
                .OrderByDescending(p => p.CreatedAtUtc).ThenByDescending(p => p.Id)
                .Take(take + 1)
                .Include(p => p.Media.OrderBy(m => m.SortOrder)).ThenInclude(m => m.File)
                .Include(p => p.Translations.Where(t => t.LanguageCode == viewerLanguage))
                .Include(p => p.Comments.OrderBy(c => c.CreatedAtUtc)).ThenInclude(c => c.Translations.Where(t => t.LanguageCode == viewerLanguage))
                .Include(p => p.Likes)
                .ToListAsync(ct);

            var hasMore = posts.Count > take;
            if (hasMore) posts = posts.Take(take).ToList();

            var authorIds = posts.Select(p => p.AuthorId)
                .Concat(posts.SelectMany(p => p.Comments.Select(c => c.AuthorId)))
                .Distinct()
                .ToList();

            var authors = await LoadAuthorsAsync(authorIds, ct);

            // Fast path: listing a page of the feed never blocks on a live translation call — it shows
            // cached translations where they exist and flags anything else as still-original so the
            // client can fetch it in the background (see GetPostAsync, used for that follow-up call).
            var result = posts.Select(p => MapPostFast(p, authors, dto.CurrentUserId, viewerLanguage)).ToList();

            return new GetFeedPageResponseDto { Posts = result, HasMore = hasMore };
        }

        public async Task<FeedPostDto?> GetPostAsync(int postId, string currentUserId, string viewerLanguage, CancellationToken ct = default)
        {
            var normalizedLanguage = AppLanguages.Normalize(viewerLanguage);

            var post = await _context.FeedPosts
                .AsNoTracking()
                .Include(p => p.Media.OrderBy(m => m.SortOrder)).ThenInclude(m => m.File)
                .Include(p => p.Translations.Where(t => t.LanguageCode == normalizedLanguage))
                .Include(p => p.Comments.OrderBy(c => c.CreatedAtUtc)).ThenInclude(c => c.Translations.Where(t => t.LanguageCode == normalizedLanguage))
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null) return null;

            var authorIds = new[] { post.AuthorId }.Concat(post.Comments.Select(c => c.AuthorId)).Distinct().ToList();
            var authors = await LoadAuthorsAsync(authorIds, ct);
            return await MapPostAsync(post, authors, currentUserId, normalizedLanguage, ct);
        }

        public async Task<FeedPostDto?> GetPostFastAsync(int postId, string currentUserId, string viewerLanguage, CancellationToken ct = default)
        {
            var normalizedLanguage = AppLanguages.Normalize(viewerLanguage);

            var post = await _context.FeedPosts
                .AsNoTracking()
                .Include(p => p.Media.OrderBy(m => m.SortOrder)).ThenInclude(m => m.File)
                .Include(p => p.Translations.Where(t => t.LanguageCode == normalizedLanguage))
                .Include(p => p.Comments.OrderBy(c => c.CreatedAtUtc)).ThenInclude(c => c.Translations.Where(t => t.LanguageCode == normalizedLanguage))
                .Include(p => p.Likes)
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null) return null;

            var authorIds = new[] { post.AuthorId }.Concat(post.Comments.Select(c => c.AuthorId)).Distinct().ToList();
            var authors = await LoadAuthorsAsync(authorIds, ct);
            return MapPostFast(post, authors, currentUserId, normalizedLanguage);
        }

        public async Task<CreateFeedPostResponseDto> CreatePostAsync(CreateFeedPostRequestDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Caption) && dto.Media.Count == 0)
                return new CreateFeedPostResponseDto { Success = false, ErrorMessage = "Skriv en historie eller vedhæft billeder/videoer." };

            var filesPath = _config["AppSettings:FilesPath"] ?? "App_files";
            var feedDir = Path.Combine(_env.ContentRootPath, filesPath, FileCategories.Feed);
            Directory.CreateDirectory(feedDir);

            var post = new FeedPost
            {
                AuthorId = dto.AuthorId,
                Caption = string.IsNullOrWhiteSpace(dto.Caption) ? null : dto.Caption.Trim(),
                OriginalLanguage = AppLanguages.Normalize(dto.AuthorLanguage),
                CreatedAtUtc = DateTime.UtcNow
            };

            var savedPaths = new List<string>();
            try
            {
                var order = 0;
                foreach (var media in dto.Media)
                {
                    var ext = Path.GetExtension(media.OriginalFileName);
                    if (string.IsNullOrWhiteSpace(ext))
                        ext = media.MediaType == FeedMediaType.Video ? ".mp4" : ".jpg";

                    var storedFileName = $"{Guid.NewGuid()}{ext}";
                    var storedRelativePath = Path.Combine(filesPath, FileCategories.Feed, storedFileName);
                    var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);

                    await using (var fileStream = File.Create(fullPath))
                    {
                        await media.Content.CopyToAsync(fileStream, ct);
                    }
                    savedPaths.Add(fullPath);

                    var fileInfo = new FileInfo(fullPath);
                    var metadata = new FileMetadata
                    {
                        OriginalFileName = media.OriginalFileName,
                        StoredFileName = storedFileName,
                        StoredPath = storedRelativePath,
                        ContentType = media.ContentType,
                        FileSizeBytes = fileInfo.Length,
                        OwnerId = dto.AuthorId,
                        Category = FileCategories.Feed,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    post.Media.Add(new FeedMedia
                    {
                        File = metadata,
                        MediaType = media.MediaType,
                        SortOrder = order++,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                _context.FeedPosts.Add(post);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths)
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                _logger.LogError(ex, "Failed to create feed post for user {UserId}", dto.AuthorId);
                return new CreateFeedPostResponseDto { Success = false, ErrorMessage = "Kunne ikke oprette opslaget." };
            }

            _logger.LogInformation("User {UserId} created feed post {PostId} with {MediaCount} media", dto.AuthorId, post.Id, post.Media.Count);
            return new CreateFeedPostResponseDto { Success = true, PostId = post.Id };
        }

        public async Task<DeleteFeedPostResponseDto> DeletePostAsync(int postId, string requestingUserId, bool isModerator, CancellationToken ct = default)
        {
            var post = await _context.FeedPosts
                .Include(p => p.Media).ThenInclude(m => m.File)
                .FirstOrDefaultAsync(p => p.Id == postId, ct);

            if (post is null)
                return new DeleteFeedPostResponseDto { Success = false, ErrorMessage = "Opslaget findes ikke." };

            if (post.AuthorId != requestingUserId && !isModerator)
                return new DeleteFeedPostResponseDto { Success = false, ErrorMessage = "Du kan ikke slette dette opslag." };

            // Remove the post first so EF's client-side cascade marks the FeedMedia rows as
            // deleted too, *before* we sever their (required, non-nullable) FK to FileMetadata
            // below — deleting FileMetadata first throws, since a still-tracked FeedMedia row
            // would be left pointing at a vanishing required principal.
            _context.FeedPosts.Remove(post); // cascades to Media/Comments/Likes

            foreach (var media in post.Media)
            {
                if (media.File is null) continue;

                var fullPath = Path.Combine(_env.ContentRootPath, media.File.StoredPath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
                _context.FileMetadata.Remove(media.File);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} deleted feed post {PostId}", requestingUserId, postId);
            return new DeleteFeedPostResponseDto { Success = true };
        }

        public async Task<GetFeedPostForEditResponseDto> GetPostForEditAsync(int postId, string requestingUserId, bool isModerator, CancellationToken ct = default)
        {
            var post = await _context.FeedPosts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == postId, ct);
            if (post is null)
                return new GetFeedPostForEditResponseDto { Success = false, ErrorMessage = "Opslaget findes ikke." };

            if (post.AuthorId != requestingUserId && !isModerator)
                return new GetFeedPostForEditResponseDto { Success = false, ErrorMessage = "Du kan ikke redigere dette opslag." };

            return new GetFeedPostForEditResponseDto
            {
                Success = true,
                PostId = post.Id,
                Caption = post.Caption,
                CreatedAtUtc = post.CreatedAtUtc,
                CanEditDate = isModerator
            };
        }

        public async Task<EditFeedPostResponseDto> EditPostAsync(EditFeedPostRequestDto dto, CancellationToken ct = default)
        {
            var post = await _context.FeedPosts
                .Include(p => p.Media)
                .Include(p => p.Translations)
                .FirstOrDefaultAsync(p => p.Id == dto.PostId, ct);

            if (post is null)
                return new EditFeedPostResponseDto { Success = false, ErrorMessage = "Opslaget findes ikke." };

            if (post.AuthorId != dto.RequestingUserId && !dto.IsModerator)
                return new EditFeedPostResponseDto { Success = false, ErrorMessage = "Du kan ikke redigere dette opslag." };

            var newCaption = string.IsNullOrWhiteSpace(dto.Caption) ? null : dto.Caption.Trim();
            if (newCaption is null && post.Media.Count == 0)
                return new EditFeedPostResponseDto { Success = false, ErrorMessage = "Opslaget skal have en historie eller vedhæftede billeder/videoer." };

            if (!string.Equals(newCaption, post.Caption, StringComparison.Ordinal))
            {
                post.Caption = newCaption;
                // The old caption's language guess and cached translations belonged to the previous
                // text — drop them so the new caption goes through verification and translation again
                // the next time it's viewed (same handling as VerifyPostLanguageAsync's own correction).
                post.IsLanguageVerified = false;
                if (post.Translations.Count > 0)
                    _context.FeedPostTranslations.RemoveRange(post.Translations);
            }

            // Only Administrator/Developer may backdate a post — enforced here, not just hidden in the
            // UI, so a direct POST from a non-moderator can't move it even if NewCreatedAtUtc is set.
            var dateChanged = false;
            if (dto.IsModerator && dto.NewCreatedAtUtc.HasValue)
            {
                var newCreatedAtUtc = DateTime.SpecifyKind(dto.NewCreatedAtUtc.Value, DateTimeKind.Utc);
                dateChanged = newCreatedAtUtc != post.CreatedAtUtc;
                post.CreatedAtUtc = newCreatedAtUtc;
            }

            post.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} edited feed post {PostId}", dto.RequestingUserId, post.Id);
            return new EditFeedPostResponseDto { Success = true, PostId = post.Id, DateChanged = dateChanged };
        }

        public async Task<AddFeedCommentResponseDto> AddCommentAsync(AddFeedCommentRequestDto dto, CancellationToken ct = default)
        {
            var postExists = await _context.FeedPosts.AnyAsync(p => p.Id == dto.PostId, ct);
            if (!postExists)
                return new AddFeedCommentResponseDto { Success = false, ErrorMessage = "Opslaget findes ikke." };

            var comment = new FeedComment
            {
                PostId = dto.PostId,
                AuthorId = dto.AuthorId,
                Body = dto.Body.Trim(),
                OriginalLanguage = AppLanguages.Normalize(dto.AuthorLanguage),
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.FeedComments.Add(comment);
            await _context.SaveChangesAsync(ct);

            return new AddFeedCommentResponseDto { Success = true, CommentId = comment.Id };
        }

        public async Task<FeedCommentDto?> GetCommentAsync(int commentId, CancellationToken ct = default)
        {
            var comment = await _context.FeedComments.AsNoTracking().FirstOrDefaultAsync(c => c.Id == commentId, ct);
            if (comment is null) return null;

            var authors = await LoadAuthorsAsync([comment.AuthorId], ct);
            authors.TryGetValue(comment.AuthorId, out var author);

            return new FeedCommentDto
            {
                Id = comment.Id,
                PostId = comment.PostId,
                AuthorId = comment.AuthorId,
                AuthorDisplayName = author?.DisplayName ?? "Ukendt bruger",
                AuthorHasAvatar = author?.HasAvatar ?? false,
                Body = comment.Body,
                IsTranslated = false,
                CreatedAtUtc = comment.CreatedAtUtc
            };
        }

        public async Task<DeleteFeedCommentResponseDto> DeleteCommentAsync(int commentId, string requestingUserId, bool isModerator, CancellationToken ct = default)
        {
            var comment = await _context.FeedComments.FirstOrDefaultAsync(c => c.Id == commentId, ct);
            if (comment is null)
                return new DeleteFeedCommentResponseDto { Success = false, ErrorMessage = "Kommentaren findes ikke." };

            if (comment.AuthorId != requestingUserId && !isModerator)
                return new DeleteFeedCommentResponseDto { Success = false, ErrorMessage = "Du kan ikke slette denne kommentar." };

            var postId = comment.PostId;
            _context.FeedComments.Remove(comment);
            await _context.SaveChangesAsync(ct);

            return new DeleteFeedCommentResponseDto { Success = true, PostId = postId };
        }

        public async Task<ToggleFeedLikeResponseDto> ToggleLikeAsync(int postId, string userId, CancellationToken ct = default)
        {
            var existing = await _context.FeedLikes.FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId, ct);

            bool liked;
            if (existing is not null)
            {
                _context.FeedLikes.Remove(existing);
                liked = false;
            }
            else
            {
                var postExists = await _context.FeedPosts.AnyAsync(p => p.Id == postId, ct);
                if (!postExists) return new ToggleFeedLikeResponseDto { Success = false };

                _context.FeedLikes.Add(new FeedLike { PostId = postId, UserId = userId, CreatedAtUtc = DateTime.UtcNow });
                liked = true;
            }

            await _context.SaveChangesAsync(ct);
            var count = await _context.FeedLikes.CountAsync(l => l.PostId == postId, ct);

            return new ToggleFeedLikeResponseDto { Success = true, Liked = liked, LikeCount = count };
        }

        public async Task<GetFeedLikersResponseDto> GetLikersAsync(int postId, CancellationToken ct = default)
        {
            var postExists = await _context.FeedPosts.AnyAsync(p => p.Id == postId, ct);
            if (!postExists) return new GetFeedLikersResponseDto { Success = false };

            var total = await _context.FeedLikes.CountAsync(l => l.PostId == postId, ct);

            var names = await _context.FeedLikes
                .AsNoTracking()
                .Where(l => l.PostId == postId)
                .OrderByDescending(l => l.CreatedAtUtc)
                .Take(FeedLimits.MaxLikerNamesInPreview)
                .Join(_context.Users, l => l.UserId, u => u.Id, (l, u) => u.DisplayName)
                .ToListAsync(ct);

            return new GetFeedLikersResponseDto { Success = true, Names = names, Total = total };
        }

        public async Task<FeedMediaFileDto?> GetMediaFileAsync(int mediaId, CancellationToken ct = default)
        {
            var media = await _context.FeedMedia
                .AsNoTracking()
                .Include(m => m.File)
                .FirstOrDefaultAsync(m => m.Id == mediaId, ct);

            if (media?.File is null) return null;

            var fullPath = Path.Combine(_env.ContentRootPath, media.File.StoredPath);
            if (!File.Exists(fullPath)) return null;

            return new FeedMediaFileDto { FullPath = fullPath, ContentType = media.File.ContentType };
        }

        private async Task<Dictionary<string, AuthorInfo>> LoadAuthorsAsync(List<string> userIds, CancellationToken ct)
        {
            if (userIds.Count == 0) return new Dictionary<string, AuthorInfo>();

            return await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => new AuthorInfo(u.DisplayName, u.ProfilePictureId.HasValue),
                    ct);
        }

        /// <summary>
        /// Maps a post for the feed listing without ever calling out to the translation service: text
        /// not already in <paramref name="viewerLanguage"/> is shown as its cached translation if one
        /// exists, or as the untouched original with <see cref="FeedPostDto.NeedsTranslation"/> set,
        /// letting the page render instantly. The client then calls Feed/Post/{id} (which routes to
        /// <see cref="MapPostAsync"/>, the slow/live path) in the background for anything flagged.
        /// </summary>
        private static FeedPostDto MapPostFast(FeedPost post, Dictionary<string, AuthorInfo> authors, string currentUserId, string viewerLanguage)
        {
            authors.TryGetValue(post.AuthorId, out var author);
            var (caption, captionTranslated, captionPending) = ResolveDisplayTextFast(post.Caption, post.OriginalLanguage, post.IsLanguageVerified, viewerLanguage, post.Translations);

            var comments = new List<FeedCommentDto>();
            var anyCommentPending = false;
            foreach (var c in post.Comments.OrderBy(c => c.CreatedAtUtc))
            {
                authors.TryGetValue(c.AuthorId, out var commentAuthor);
                var (body, bodyTranslated, bodyPending) = ResolveDisplayTextFast(c.Body, c.OriginalLanguage, c.IsLanguageVerified, viewerLanguage, c.Translations);
                anyCommentPending |= bodyPending;

                comments.Add(new FeedCommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    AuthorId = c.AuthorId,
                    AuthorDisplayName = commentAuthor?.DisplayName ?? "Ukendt bruger",
                    AuthorHasAvatar = commentAuthor?.HasAvatar ?? false,
                    Body = body ?? string.Empty,
                    IsTranslated = bodyTranslated,
                    CreatedAtUtc = c.CreatedAtUtc
                });
            }

            return new FeedPostDto
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorDisplayName = author?.DisplayName ?? "Ukendt bruger",
                AuthorHasAvatar = author?.HasAvatar ?? false,
                Caption = caption,
                IsCaptionTranslated = captionTranslated,
                NeedsTranslation = captionPending || anyCommentPending,
                CreatedAtUtc = post.CreatedAtUtc,
                Media = post.Media
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new FeedMediaDto
                    {
                        Id = m.Id,
                        IsVideo = m.MediaType == FeedMediaType.Video,
                        ContentType = m.File?.ContentType ?? string.Empty
                    })
                    .ToList(),
                LikeCount = post.Likes.Count,
                IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
                Comments = comments
            };
        }

        private static (string? Text, bool IsTranslated, bool NeedsTranslation) ResolveDisplayTextFast(
            string? originalText, string originalLanguage, bool isLanguageVerified, string viewerLanguage, IEnumerable<FeedPostTranslation> cachedTranslations)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return (originalText, false, false);

            // OriginalLanguage is only a guess (the author's profile setting) until it's been checked
            // against the actual text once — until then, always route through the live path even if
            // the guess already happens to match the viewer, since the guess itself might be wrong.
            if (!isLanguageVerified)
                return (originalText, false, true);

            if (string.Equals(originalLanguage, viewerLanguage, StringComparison.OrdinalIgnoreCase))
                return (originalText, false, false);

            var cached = cachedTranslations.FirstOrDefault(t => t.LanguageCode == viewerLanguage);
            return cached is not null ? (cached.TranslatedText, true, false) : (originalText, false, true);
        }

        private static (string? Text, bool IsTranslated, bool NeedsTranslation) ResolveDisplayTextFast(
            string? originalText, string originalLanguage, bool isLanguageVerified, string viewerLanguage, IEnumerable<FeedCommentTranslation> cachedTranslations)
        {
            if (string.IsNullOrWhiteSpace(originalText))
                return (originalText, false, false);

            if (!isLanguageVerified)
                return (originalText, false, true);

            if (string.Equals(originalLanguage, viewerLanguage, StringComparison.OrdinalIgnoreCase))
                return (originalText, false, false);

            var cached = cachedTranslations.FirstOrDefault(t => t.LanguageCode == viewerLanguage);
            return cached is not null ? (cached.TranslatedText, true, false) : (originalText, false, true);
        }

        /// <summary>Live path: called only for a single post at a time (Feed/Post/{id}), translating anything not already cached.</summary>
        private async Task<FeedPostDto> MapPostAsync(FeedPost post, Dictionary<string, AuthorInfo> authors, string currentUserId, string viewerLanguage, CancellationToken ct)
        {
            authors.TryGetValue(post.AuthorId, out var author);
            var (caption, captionTranslated) = await ResolvePostCaptionAsync(post, viewerLanguage, ct);

            var comments = new List<FeedCommentDto>();
            foreach (var c in post.Comments.OrderBy(c => c.CreatedAtUtc))
            {
                authors.TryGetValue(c.AuthorId, out var commentAuthor);
                var (body, bodyTranslated) = await ResolveCommentBodyAsync(c, viewerLanguage, ct);

                comments.Add(new FeedCommentDto
                {
                    Id = c.Id,
                    PostId = c.PostId,
                    AuthorId = c.AuthorId,
                    AuthorDisplayName = commentAuthor?.DisplayName ?? "Ukendt bruger",
                    AuthorHasAvatar = commentAuthor?.HasAvatar ?? false,
                    Body = body,
                    IsTranslated = bodyTranslated,
                    CreatedAtUtc = c.CreatedAtUtc
                });
            }

            return new FeedPostDto
            {
                Id = post.Id,
                AuthorId = post.AuthorId,
                AuthorDisplayName = author?.DisplayName ?? "Ukendt bruger",
                AuthorHasAvatar = author?.HasAvatar ?? false,
                Caption = caption,
                IsCaptionTranslated = captionTranslated,
                CreatedAtUtc = post.CreatedAtUtc,
                Media = post.Media
                    .OrderBy(m => m.SortOrder)
                    .Select(m => new FeedMediaDto
                    {
                        Id = m.Id,
                        IsVideo = m.MediaType == FeedMediaType.Video,
                        ContentType = m.File?.ContentType ?? string.Empty
                    })
                    .ToList(),
                LikeCount = post.Likes.Count,
                IsLikedByCurrentUser = post.Likes.Any(l => l.UserId == currentUserId),
                Comments = comments
            };
        }

        /// <summary>
        /// Resolves the caption to show this viewer: unchanged if it's already in their language, a cached
        /// translation if <c>post.Translations</c> already holds one for it (the caller's query pre-filters
        /// that collection to <paramref name="viewerLanguage"/>), or a freshly machine-translated copy —
        /// which is then cached so the same post is never re-translated for that language again. Falls back
        /// to the original text, untagged, if translation isn't available (e.g. AiGateway isn't configured),
        /// so a post is never hidden just because it couldn't be translated.
        /// </summary>
        private async Task<(string? Text, bool IsTranslated)> ResolvePostCaptionAsync(FeedPost post, string viewerLanguage, CancellationToken ct)
        {
            if (!post.IsLanguageVerified)
                await VerifyPostLanguageAsync(post, ct);

            var original = post.Caption;
            if (string.IsNullOrWhiteSpace(original) || string.Equals(post.OriginalLanguage, viewerLanguage, StringComparison.OrdinalIgnoreCase))
                return (original, false);

            var cached = post.Translations.FirstOrDefault(t => t.LanguageCode == viewerLanguage);
            if (cached is not null)
                return (cached.TranslatedText, true);

            var translated = await TryTranslateAsync(original, post.OriginalLanguage, viewerLanguage, ct);
            if (translated is null)
                return (original, false);

            var entity = new FeedPostTranslation
            {
                PostId = post.Id,
                LanguageCode = viewerLanguage,
                TranslatedText = translated,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.FeedPostTranslations.Add(entity);
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Detach so this failed insert doesn't poison the next SaveChangesAsync call in this
                // request's mapping loop — see TryTranslateAsync's caller for why this can happen.
                _context.Entry(entity).State = EntityState.Detached;
                _logger.LogDebug(ex, "Feed post translation cache write skipped for post {PostId}/{Language} (likely already cached by a concurrent request)", post.Id, viewerLanguage);
            }

            return (translated, true);
        }

        /// <summary>Same as <see cref="ResolvePostCaptionAsync"/>, for a comment's body.</summary>
        private async Task<(string Text, bool IsTranslated)> ResolveCommentBodyAsync(FeedComment comment, string viewerLanguage, CancellationToken ct)
        {
            if (!comment.IsLanguageVerified)
                await VerifyCommentLanguageAsync(comment, ct);

            var original = comment.Body;
            if (string.Equals(comment.OriginalLanguage, viewerLanguage, StringComparison.OrdinalIgnoreCase))
                return (original, false);

            var cached = comment.Translations.FirstOrDefault(t => t.LanguageCode == viewerLanguage);
            if (cached is not null)
                return (cached.TranslatedText, true);

            var translated = await TryTranslateAsync(original, comment.OriginalLanguage, viewerLanguage, ct);
            if (translated is null)
                return (original, false);

            var entity = new FeedCommentTranslation
            {
                CommentId = comment.Id,
                LanguageCode = viewerLanguage,
                TranslatedText = translated,
                CreatedAtUtc = DateTime.UtcNow
            };
            _context.FeedCommentTranslations.Add(entity);
            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                _context.Entry(entity).State = EntityState.Detached;
                _logger.LogDebug(ex, "Feed comment translation cache write skipped for comment {CommentId}/{Language} (likely already cached by a concurrent request)", comment.Id, viewerLanguage);
            }

            return (translated, true);
        }

        // Two concurrent viewers with the same preferred language can both miss the cache and both try to
        // translate + insert the same (PostId/CommentId, LanguageCode) row; the unique index rejects the
        // second insert. That's fine — that request still shows its own caller the freshly translated text,
        // it just doesn't also win the race to cache it (handled by the callers above).
        private async Task<string?> TryTranslateAsync(string text, string sourceLanguageCode, string targetLanguageCode, CancellationToken ct)
        {
            try
            {
                var sourceNative = AppLanguages.GetNativeName(sourceLanguageCode);
                var targetNative = AppLanguages.GetNativeName(targetLanguageCode);
                var model = await ResolveTranslationModelAsync(ct);
                var translated = await _language.TranslateAsync(text, targetNative, sourceNative, model: model, cancellationToken: ct);
                return string.IsNullOrWhiteSpace(translated) ? null : translated;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Feed translation failed ({Source} -> {Target})", sourceLanguageCode, targetLanguageCode);
                return null;
            }
        }

        /// <summary>
        /// Checks a post's OriginalLanguage (so far only a guess: the author's preferred-language
        /// setting at post time, which is what they usually — but not always — write in) against the
        /// actual caption text, correcting it if wrong, then marks it verified so this only ever runs
        /// once. A wrong original language would otherwise permanently hide the caption from
        /// translation whenever it happens to already match a viewer's language by coincidence.
        /// </summary>
        private async Task VerifyPostLanguageAsync(FeedPost post, CancellationToken ct)
        {
            var detected = string.IsNullOrWhiteSpace(post.Caption) ? null : await TryDetectLanguageCodeAsync(post.Caption, ct);
            var corrected = detected is not null && !string.Equals(detected, post.OriginalLanguage, StringComparison.OrdinalIgnoreCase);
            var newLanguage = corrected ? detected! : post.OriginalLanguage;

            try
            {
                if (corrected)
                {
                    // Any translation cached so far was made assuming the wrong source language —
                    // drop it so it's redone correctly, from the now-known language, when next needed.
                    await _context.FeedPostTranslations.Where(t => t.PostId == post.Id).ExecuteDeleteAsync(ct);
                }

                await _context.FeedPosts.Where(p => p.Id == post.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.IsLanguageVerified, true)
                        .SetProperty(p => p.OriginalLanguage, newLanguage), ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Feed post language verification write skipped for post {PostId}", post.Id);
            }

            post.IsLanguageVerified = true;
            post.OriginalLanguage = newLanguage;
            if (corrected) post.Translations.Clear();
        }

        /// <summary>Same as <see cref="VerifyPostLanguageAsync"/>, for a comment's body.</summary>
        private async Task VerifyCommentLanguageAsync(FeedComment comment, CancellationToken ct)
        {
            var detected = await TryDetectLanguageCodeAsync(comment.Body, ct);
            var corrected = detected is not null && !string.Equals(detected, comment.OriginalLanguage, StringComparison.OrdinalIgnoreCase);
            var newLanguage = corrected ? detected! : comment.OriginalLanguage;

            try
            {
                if (corrected)
                    await _context.FeedCommentTranslations.Where(t => t.CommentId == comment.Id).ExecuteDeleteAsync(ct);

                await _context.FeedComments.Where(c => c.Id == comment.Id)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(c => c.IsLanguageVerified, true)
                        .SetProperty(c => c.OriginalLanguage, newLanguage), ct);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Feed comment language verification write skipped for comment {CommentId}", comment.Id);
            }

            comment.IsLanguageVerified = true;
            comment.OriginalLanguage = newLanguage;
            if (corrected) comment.Translations.Clear();
        }

        /// <summary>
        /// Detects the actual language of <paramref name="text"/> via LanguageTools.DetectLanguageAsync
        /// (which answers with the Danish name of the language, e.g. "Engelsk") and maps that back to
        /// an web.Constants.AppLanguages code. Returns null — leaving the existing guess in place — if
        /// detection fails, the text is empty, or the answer doesn't match a known language name.
        /// </summary>
        private async Task<string?> TryDetectLanguageCodeAsync(string text, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            try
            {
                var model = await ResolveTranslationModelAsync(ct);
                return await _language.DetectLanguageCodeAsync(text, model: model, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Feed language detection failed");
                return null;
            }
        }
    }
}
