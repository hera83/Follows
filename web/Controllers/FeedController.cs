using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Feed.Dtos;
using web.Repositories.Feed.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class FeedController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFeedService _feedService;

        public FeedController(
            UserManager<ApplicationUser> userManager,
            IFeedService feedService)
        {
            _userManager = userManager;
            _feedService = feedService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var isModerator = IsModerator();
            var viewerLanguage = AppLanguages.Normalize(user.PreferredLanguage);
            var labels = FeedTranslationStrings.GetLabels(viewerLanguage);
            var page = await _feedService.GetFeedPageAsync(new GetFeedPageRequestDto
            {
                CurrentUserId = user.Id,
                ViewerLanguage = viewerLanguage,
                Take = 10
            }, HttpContext.RequestAborted);

            var vm = new FeedIndexViewModel
            {
                Posts = page.Posts.Select(p => MapToViewModel(p, user.Id, isModerator, labels)).ToList(),
                HasMore = page.HasMore,
                NextBeforeId = page.Posts.Count > 0 ? page.Posts[^1].Id : null,
                CurrentUserId = user.Id,
                CurrentUserDisplayName = user.DisplayName,
                CurrentUserHasAvatar = user.ProfilePictureId.HasValue,
                CurrentUserIsModerator = isModerator,
                TranslatingBannerFormat = labels.TranslatingBannerFormat,
                TranslatingLabel = labels.Translating
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> LoadMore(int beforeId, int take = 10)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var isModerator = IsModerator();
            var viewerLanguage = AppLanguages.Normalize(user.PreferredLanguage);
            var labels = FeedTranslationStrings.GetLabels(viewerLanguage);
            var page = await _feedService.GetFeedPageAsync(new GetFeedPageRequestDto
            {
                CurrentUserId = user.Id,
                ViewerLanguage = viewerLanguage,
                BeforeId = beforeId,
                Take = take
            }, HttpContext.RequestAborted);

            var vm = new FeedIndexViewModel
            {
                Posts = page.Posts.Select(p => MapToViewModel(p, user.Id, isModerator, labels)).ToList(),
                HasMore = page.HasMore,
                NextBeforeId = page.Posts.Count > 0 ? page.Posts[^1].Id : (int?)null
            };

            return PartialView("_FeedPostList", vm);
        }

        [HttpGet]
        public async Task<IActionResult> Post(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var viewerLanguage = AppLanguages.Normalize(user.PreferredLanguage);
            var dto = await _feedService.GetPostAsync(id, user.Id, viewerLanguage, HttpContext.RequestAborted);
            if (dto is null) return NotFound();

            var vm = MapToViewModel(dto, user.Id, IsModerator(), FeedTranslationStrings.GetLabels(viewerLanguage));
            return PartialView("_FeedPostCard", vm);
        }

        /// <summary>
        /// Same as Post/{id}, but never calls the translation service (see GetPostFastAsync) — used to
        /// show a post the current user just created, which can't possibly need translating for them.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> PostFast(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var viewerLanguage = AppLanguages.Normalize(user.PreferredLanguage);
            var dto = await _feedService.GetPostFastAsync(id, user.Id, viewerLanguage, HttpContext.RequestAborted);
            if (dto is null) return NotFound();

            var vm = MapToViewModel(dto, user.Id, IsModerator(), FeedTranslationStrings.GetLabels(viewerLanguage));
            return PartialView("_FeedPostCard", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(FeedLimits.MaxTotalBytesPerPost + 5_000_000)]
        public async Task<IActionResult> Create(CreateFeedPostViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Opslaget kunne ikke oprettes." : errors);
            }

            var files = model.Files ?? new List<IFormFile>();

            if (string.IsNullOrWhiteSpace(model.Caption) && files.Count == 0)
                return this.ToastErrorJson("Skriv en historie eller vedhæft billeder/videoer.");

            if (files.Count > FeedLimits.MaxFilesPerPost)
                return this.ToastErrorJson($"Du kan højst vedhæfte {FeedLimits.MaxFilesPerPost} filer pr. opslag.");

            var mediaInputs = new List<FeedMediaInputDto>();
            var streams = new List<Stream>();
            try
            {
                long totalBytes = 0;
                foreach (var file in files)
                {
                    if (file.Length == 0) continue;

                    var isImage = FeedLimits.AllowedImageContentTypes.Contains(file.ContentType);
                    var isVideo = FeedLimits.AllowedVideoContentTypes.Contains(file.ContentType);
                    if (!isImage && !isVideo)
                        return this.ToastErrorJson($"Filtypen for '{file.FileName}' er ikke understøttet.");

                    var maxBytes = isVideo ? FeedLimits.MaxVideoBytes : FeedLimits.MaxImageBytes;
                    if (file.Length > maxBytes)
                    {
                        return this.ToastErrorJson(isVideo
                            ? $"'{file.FileName}' er for stor (max 300 MB pr. video)."
                            : $"'{file.FileName}' er for stor (max 25 MB pr. billede).");
                    }

                    totalBytes += file.Length;
                    if (totalBytes > FeedLimits.MaxTotalBytesPerPost)
                        return this.ToastErrorJson("Den samlede filstørrelse for opslaget er for stor.");

                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    mediaInputs.Add(new FeedMediaInputDto
                    {
                        Content = stream,
                        ContentType = file.ContentType,
                        OriginalFileName = file.FileName,
                        MediaType = isVideo ? FeedMediaType.Video : FeedMediaType.Image
                    });
                }

                var result = await _feedService.CreatePostAsync(new CreateFeedPostRequestDto
                {
                    AuthorId = user.Id,
                    AuthorLanguage = AppLanguages.Normalize(user.PreferredLanguage),
                    Caption = model.Caption,
                    Media = mediaInputs
                }, HttpContext.RequestAborted);

                if (!result.Success)
                    return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke oprette opslaget.");

                return Json(new { success = true, message = "Historie delt.", type = "success", postId = result.PostId });
            }
            finally
            {
                foreach (var s in streams) await s.DisposeAsync();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Comment(AddFeedCommentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Kommentaren kunne ikke tilføjes." : errors);
            }

            var result = await _feedService.AddCommentAsync(new AddFeedCommentRequestDto
            {
                PostId = model.PostId,
                AuthorId = user.Id,
                AuthorLanguage = AppLanguages.Normalize(user.PreferredLanguage),
                Body = model.Body
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Kommentaren kunne ikke tilføjes.");

            return Json(new { success = true, message = "Kommentar tilføjet.", type = "success", postId = model.PostId, commentId = result.CommentId });
        }

        /// <summary>
        /// Shows one comment as-is, with no translation attempt — used to instantly render a commenter
        /// their own just-posted comment (see IFeedService.GetCommentAsync).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CommentItem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var dto = await _feedService.GetCommentAsync(id, HttpContext.RequestAborted);
            if (dto is null) return NotFound();

            var labels = FeedTranslationStrings.GetLabels(user.PreferredLanguage);
            var vm = MapCommentToViewModel(dto, user.Id, IsModerator(), labels);
            return PartialView("_FeedCommentItem", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _feedService.ToggleLikeAsync(postId, user.Id, HttpContext.RequestAborted);
            if (!result.Success) return NotFound();

            return Json(new { success = true, liked = result.Liked, likeCount = result.LikeCount });
        }

        [HttpGet]
        public async Task<IActionResult> Likers(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _feedService.GetLikersAsync(id, HttpContext.RequestAborted);
            if (!result.Success) return NotFound();

            return Json(new { success = true, names = result.Names, total = result.Total });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _feedService.DeletePostAsync(postId, user.Id, IsModerator(), HttpContext.RequestAborted);

            return result.Success
                ? this.ToastSuccessJson("Opslaget er slettet.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke slette opslaget.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _feedService.DeleteCommentAsync(commentId, user.Id, IsModerator(), HttpContext.RequestAborted);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke slette kommentaren.");

            return Json(new { success = true, message = "Kommentar slettet.", type = "success", postId = result.PostId });
        }

        /// <summary>Populates the edit-post modal with the post's original (untranslated) caption and date.</summary>
        [HttpGet]
        public async Task<IActionResult> EditPostData(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _feedService.GetPostForEditAsync(id, user.Id, IsModerator(), HttpContext.RequestAborted);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Opslaget kunne ikke hentes.");

            return Json(new
            {
                success = true,
                postId = result.PostId,
                caption = result.Caption,
                createdAtUtc = result.CreatedAtUtc.ToString("o"),
                canEditDate = result.CanEditDate
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(EditFeedPostViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Opslaget kunne ikke gemmes." : errors);
            }

            var result = await _feedService.EditPostAsync(new EditFeedPostRequestDto
            {
                PostId = model.PostId,
                RequestingUserId = user.Id,
                IsModerator = IsModerator(),
                Caption = model.Caption,
                NewCreatedAtUtc = model.CreatedAtUtc?.UtcDateTime
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Opslaget kunne ikke gemmes.");

            return Json(new { success = true, message = "Opslaget er opdateret.", type = "success", postId = result.PostId, dateChanged = result.DateChanged });
        }

        [HttpGet]
        public async Task<IActionResult> Media(int id)
        {
            var file = await _feedService.GetMediaFileAsync(id, HttpContext.RequestAborted);
            if (file is null) return NotFound();

            return PhysicalFile(file.FullPath, file.ContentType, enableRangeProcessing: true);
        }

        private bool IsModerator() =>
            User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

        private static FeedPostViewModel MapToViewModel(FeedPostDto dto, string currentUserId, bool isModerator, FeedTranslationLabels labels) => new()
        {
            Id = dto.Id,
            AuthorId = dto.AuthorId,
            AuthorDisplayName = dto.AuthorDisplayName,
            AuthorHasAvatar = dto.AuthorHasAvatar,
            Caption = dto.Caption,
            IsTranslated = dto.IsCaptionTranslated,
            NeedsTranslation = dto.NeedsTranslation,
            TranslatedLabel = labels.Translated,
            TranslatingLabel = labels.Translating,
            CreatedAtUtc = dto.CreatedAtUtc,
            Media = dto.Media.Select(m => new FeedMediaViewModel { Id = m.Id, IsVideo = m.IsVideo, ContentType = m.ContentType }).ToList(),
            LikeCount = dto.LikeCount,
            IsLikedByCurrentUser = dto.IsLikedByCurrentUser,
            CanDelete = dto.AuthorId == currentUserId || isModerator,
            CanEdit = dto.AuthorId == currentUserId || isModerator,
            CanEditDate = isModerator,
            Comments = dto.Comments.Select(c => MapCommentToViewModel(c, currentUserId, isModerator, labels)).ToList()
        };

        private static FeedCommentViewModel MapCommentToViewModel(FeedCommentDto dto, string currentUserId, bool isModerator, FeedTranslationLabels labels) => new()
        {
            Id = dto.Id,
            PostId = dto.PostId,
            AuthorId = dto.AuthorId,
            AuthorDisplayName = dto.AuthorDisplayName,
            AuthorHasAvatar = dto.AuthorHasAvatar,
            Body = dto.Body,
            IsTranslated = dto.IsTranslated,
            TranslatedLabel = labels.Translated,
            CreatedAtUtc = dto.CreatedAtUtc,
            CanDelete = dto.AuthorId == currentUserId || isModerator
        };
    }
}
