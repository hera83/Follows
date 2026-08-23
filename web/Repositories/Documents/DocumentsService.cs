using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Documents.Dtos;
using web.Repositories.Documents.Interfaces;
using web.Services.AiGateway.Interfaces;

namespace web.Repositories.Documents
{
    public class DocumentsService : IDocumentsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DocumentsService> _logger;
        private readonly LanguageTools _language;

        public DocumentsService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<DocumentsService> logger,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
            _language = aiGatewayService.Language(aiGatewayConfigurationProvider);
        }

        public async Task<List<DocumentGroupDto>> GetGroupsAsync(CancellationToken ct = default)
        {
            return await _context.DocumentGroups
                .AsNoTracking()
                .OrderBy(g => g.Name)
                .Select(g => new DocumentGroupDto
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description,
                    CreatedByUserId = g.CreatedByUserId,
                    CreatedAtUtc = g.CreatedAtUtc,
                    DocumentCount = g.Documents.Count
                })
                .ToListAsync(ct);
        }

        public async Task<DocumentGroupDetailDto?> GetGroupDetailAsync(int groupId, string currentUserId, bool isModerator, CancellationToken ct = default)
        {
            var group = await _context.DocumentGroups
                .AsNoTracking()
                .Include(g => g.Documents.OrderByDescending(d => d.CreatedAtUtc)).ThenInclude(d => d.File)
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group is null) return null;

            var uploaderIds = group.Documents.Select(d => d.UploadedByUserId).Distinct().ToList();
            var uploaders = await LoadDisplayNamesAsync(uploaderIds, ct);

            return new DocumentGroupDetailDto
            {
                Id = group.Id,
                Name = group.Name,
                Description = group.Description,
                CanManage = group.CreatedByUserId == currentUserId || isModerator,
                Documents = group.Documents.Select(d => new DocumentDto
                {
                    Id = d.Id,
                    GroupId = d.GroupId,
                    Title = d.Title,
                    ContentType = d.File?.ContentType ?? string.Empty,
                    FileSizeBytes = d.File?.FileSizeBytes ?? 0,
                    UploadedByUserId = d.UploadedByUserId,
                    UploadedByDisplayName = uploaders.GetValueOrDefault(d.UploadedByUserId, "Ukendt bruger"),
                    CreatedAtUtc = d.CreatedAtUtc,
                    CanPreviewInline = DocumentLimits.CanPreviewInline(d.File?.ContentType ?? string.Empty),
                    CanTranslate = DocumentLimits.CanExtractText(d.File?.ContentType ?? string.Empty),
                    CanDelete = d.UploadedByUserId == currentUserId || isModerator
                }).ToList()
            };
        }

        public async Task<CreateDocumentGroupResponseDto> CreateGroupAsync(CreateDocumentGroupRequestDto dto, CancellationToken ct = default)
        {
            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new CreateDocumentGroupResponseDto { Success = false, ErrorMessage = "Angiv et gruppenavn." };

            var exists = await _context.DocumentGroups.AnyAsync(g => g.Name == name, ct);
            if (exists)
                return new CreateDocumentGroupResponseDto { Success = false, ErrorMessage = $"Gruppen '{name}' findes allerede." };

            var group = new DocumentGroup
            {
                Name = name,
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                CreatedByUserId = dto.CreatedByUserId,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.DocumentGroups.Add(group);
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} created document group {GroupId} ({Name})", dto.CreatedByUserId, group.Id, group.Name);
            return new CreateDocumentGroupResponseDto { Success = true, GroupId = group.Id };
        }

        public async Task<UpdateDocumentGroupResponseDto> UpdateGroupAsync(UpdateDocumentGroupRequestDto dto, CancellationToken ct = default)
        {
            var group = await _context.DocumentGroups.FirstOrDefaultAsync(g => g.Id == dto.GroupId, ct);
            if (group is null)
                return new UpdateDocumentGroupResponseDto { Success = false, ErrorMessage = "Gruppen findes ikke." };

            if (group.CreatedByUserId != dto.RequestingUserId && !dto.IsModerator)
                return new UpdateDocumentGroupResponseDto { Success = false, ErrorMessage = "Du kan ikke redigere denne gruppe." };

            var name = dto.Name?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name))
                return new UpdateDocumentGroupResponseDto { Success = false, ErrorMessage = "Angiv et gruppenavn." };

            var duplicate = await _context.DocumentGroups.AnyAsync(g => g.Id != dto.GroupId && g.Name == name, ct);
            if (duplicate)
                return new UpdateDocumentGroupResponseDto { Success = false, ErrorMessage = $"Gruppen '{name}' findes allerede." };

            group.Name = name;
            group.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} renamed document group {GroupId} to {Name}", dto.RequestingUserId, group.Id, group.Name);
            return new UpdateDocumentGroupResponseDto { Success = true };
        }

        public async Task<DeleteDocumentGroupResponseDto> DeleteGroupAsync(int groupId, string requestingUserId, bool isModerator, CancellationToken ct = default)
        {
            var group = await _context.DocumentGroups
                .Include(g => g.Documents).ThenInclude(d => d.File)
                .FirstOrDefaultAsync(g => g.Id == groupId, ct);

            if (group is null)
                return new DeleteDocumentGroupResponseDto { Success = false, ErrorMessage = "Gruppen findes ikke." };

            if (group.CreatedByUserId != requestingUserId && !isModerator)
                return new DeleteDocumentGroupResponseDto { Success = false, ErrorMessage = "Du kan ikke slette denne gruppe." };

            // Remove the group first so EF's client-side cascade marks the Document rows as deleted
            // too, *before* we sever their (required, non-nullable) FK to FileMetadata below —
            // deleting FileMetadata first throws, since a still-tracked Document row would be left
            // pointing at a vanishing required principal (same reasoning as FeedService.DeletePostAsync).
            _context.DocumentGroups.Remove(group); // cascades to Documents

            foreach (var document in group.Documents)
            {
                if (document.File is null) continue;

                var fullPath = Path.Combine(_env.ContentRootPath, document.File.StoredPath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
                _context.FileMetadata.Remove(document.File);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} deleted document group {GroupId} ({DocumentCount} documents)", requestingUserId, groupId, group.Documents.Count);
            return new DeleteDocumentGroupResponseDto { Success = true };
        }

        public async Task<UploadDocumentsResponseDto> UploadDocumentsAsync(UploadDocumentsRequestDto dto, CancellationToken ct = default)
        {
            if (dto.Files.Count == 0)
                return new UploadDocumentsResponseDto { Success = false, ErrorMessage = "Vælg mindst én fil." };

            var groupExists = await _context.DocumentGroups.AnyAsync(g => g.Id == dto.GroupId, ct);
            if (!groupExists)
                return new UploadDocumentsResponseDto { Success = false, ErrorMessage = "Gruppen findes ikke." };

            var filesPath = _config["AppSettings:FilesPath"] ?? "App_files";
            var documentsDir = Path.Combine(_env.ContentRootPath, filesPath, FileCategories.Documents);
            Directory.CreateDirectory(documentsDir);

            var savedPaths = new List<string>();
            var documents = new List<Document>();
            try
            {
                foreach (var file in dto.Files)
                {
                    var ext = Path.GetExtension(file.OriginalFileName);
                    var storedFileName = $"{Guid.NewGuid()}{ext}";
                    var storedRelativePath = Path.Combine(filesPath, FileCategories.Documents, storedFileName);
                    var fullPath = Path.Combine(_env.ContentRootPath, storedRelativePath);

                    await using (var fileStream = File.Create(fullPath))
                    {
                        await file.Content.CopyToAsync(fileStream, ct);
                    }
                    savedPaths.Add(fullPath);

                    var fileInfo = new FileInfo(fullPath);
                    var metadata = new FileMetadata
                    {
                        OriginalFileName = file.OriginalFileName,
                        StoredFileName = storedFileName,
                        StoredPath = storedRelativePath,
                        ContentType = file.ContentType,
                        FileSizeBytes = fileInfo.Length,
                        OwnerId = dto.UploadedByUserId,
                        Category = FileCategories.Documents,
                        CreatedAtUtc = DateTime.UtcNow
                    };

                    documents.Add(new Document
                    {
                        GroupId = dto.GroupId,
                        File = metadata,
                        Title = file.OriginalFileName,
                        UploadedByUserId = dto.UploadedByUserId,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                _context.Documents.AddRange(documents);
                await _context.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                foreach (var path in savedPaths)
                {
                    if (File.Exists(path)) File.Delete(path);
                }
                _logger.LogError(ex, "Failed to upload documents to group {GroupId} for user {UserId}", dto.GroupId, dto.UploadedByUserId);
                return new UploadDocumentsResponseDto { Success = false, ErrorMessage = "Kunne ikke uploade filerne." };
            }

            _logger.LogInformation("User {UserId} uploaded {Count} document(s) to group {GroupId}", dto.UploadedByUserId, documents.Count, dto.GroupId);
            return new UploadDocumentsResponseDto { Success = true, UploadedCount = documents.Count };
        }

        public async Task<DeleteDocumentResponseDto> DeleteDocumentAsync(int documentId, string requestingUserId, bool isModerator, CancellationToken ct = default)
        {
            var document = await _context.Documents
                .Include(d => d.File)
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            if (document is null)
                return new DeleteDocumentResponseDto { Success = false, ErrorMessage = "Dokumentet findes ikke." };

            if (document.UploadedByUserId != requestingUserId && !isModerator)
                return new DeleteDocumentResponseDto { Success = false, ErrorMessage = "Du kan ikke slette dette dokument." };

            var groupId = document.GroupId;
            _context.Documents.Remove(document);

            if (document.File is not null)
            {
                var fullPath = Path.Combine(_env.ContentRootPath, document.File.StoredPath);
                if (File.Exists(fullPath)) File.Delete(fullPath);
                _context.FileMetadata.Remove(document.File);
            }

            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("User {UserId} deleted document {DocumentId}", requestingUserId, documentId);
            return new DeleteDocumentResponseDto { Success = true, GroupId = groupId };
        }

        public async Task<DocumentFileDto?> GetDocumentFileAsync(int documentId, CancellationToken ct = default)
        {
            var document = await _context.Documents
                .AsNoTracking()
                .Include(d => d.File)
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            if (document?.File is null) return null;

            var fullPath = Path.Combine(_env.ContentRootPath, document.File.StoredPath);
            if (!File.Exists(fullPath)) return null;

            return new DocumentFileDto
            {
                FullPath = fullPath,
                ContentType = document.File.ContentType,
                OriginalFileName = document.File.OriginalFileName
            };
        }

        /// <summary>
        /// Extracts the document's text, and — unless it's already written in <paramref name="preferredLanguageCode"/> —
        /// translates it there and formats it as Markdown, rendered to sanitized HTML for the preview modal.
        /// Results are cached per (document, language): documents are immutable once uploaded, so a document
        /// only ever goes through extraction + translation once per language — unless <paramref name="force"/>
        /// is set (the preview modal's "Genoversæt"-button), which skips the cache and overwrites it with a
        /// freshly translated result. Useful when a first attempt came out empty/partial (e.g. from the
        /// model-reasoning issue chunking guards against) and the document is worth simply trying again.
        /// </summary>
        public async Task<TranslateDocumentResponseDto> TranslateDocumentAsync(
            int documentId,
            string preferredLanguageCode,
            bool force = false,
            Action<int>? onChunkCountKnown = null,
            Action<int>? onProgress = null,
            CancellationToken ct = default)
        {
            var document = await _context.Documents
                .AsNoTracking()
                .Include(d => d.File)
                .FirstOrDefaultAsync(d => d.Id == documentId, ct);

            if (document?.File is null)
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Dokumentet findes ikke." };

            var contentType = document.File.ContentType;
            if (!DocumentLimits.CanExtractText(contentType))
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Oversættelse understøttes ikke for denne filtype." };

            var fullPath = Path.Combine(_env.ContentRootPath, document.File.StoredPath);
            if (!File.Exists(fullPath))
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Filen kunne ikke findes på serveren." };

            var targetLanguageCode = AppLanguages.Normalize(preferredLanguageCode);
            var targetNative = AppLanguages.GetNativeName(targetLanguageCode);

            // Tracked (not AsNoTracking): a force-retry reuses this same row and updates it in place below,
            // instead of inserting a second row that the (DocumentId, LanguageCode) unique index would reject.
            var existingTranslation = await _context.DocumentTranslations
                .FirstOrDefaultAsync(t => t.DocumentId == documentId && t.LanguageCode == targetLanguageCode, ct);
            if (existingTranslation is not null && !force)
            {
                return new TranslateDocumentResponseDto
                {
                    Success = true,
                    Html = MarkdownRenderer.ToSafeHtml(existingTranslation.TranslatedMarkdown),
                    TargetLanguageName = targetNative
                };
            }

            string extracted;
            try
            {
                extracted = DocumentMarkdownExtractor.Extract(fullPath, contentType);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract text from document {DocumentId}", documentId);
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Kunne ikke læse indholdet af dokumentet." };
            }

            if (string.IsNullOrWhiteSpace(extracted))
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Der blev ikke fundet nogen tekst i dokumentet." };

            var truncated = false;
            if (extracted.Length > DocumentLimits.MaxTranslatableChars)
            {
                extracted = extracted[..DocumentLimits.MaxTranslatableChars];
                truncated = true;
            }

            string? sourceLanguageCode;
            try
            {
                sourceLanguageCode = await _language.DetectLanguageCodeAsync(extracted, cancellationToken: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Language detection failed for document {DocumentId}", documentId);
                sourceLanguageCode = null;
            }

            if (sourceLanguageCode is not null && string.Equals(sourceLanguageCode, targetLanguageCode, StringComparison.OrdinalIgnoreCase))
            {
                return new TranslateDocumentResponseDto
                {
                    Success = true,
                    AlreadyInTargetLanguage = true,
                    TargetLanguageName = targetNative
                };
            }

            string markdown;
            try
            {
                var sourceNative = sourceLanguageCode is not null ? AppLanguages.GetNativeName(sourceLanguageCode) : null;

                // Translated in chunks, not as one giant prompt — a whole long document easily exceeds
                // the model's context window, and Ollama just stops generating silently when that
                // happens (no error), which is what previously showed up as only the first third or so
                // of a document getting translated. Each chunk is small enough to always complete.
                var chunks = SplitIntoChunks(extracted, DocumentLimits.TranslationChunkChars);
                onChunkCountKnown?.Invoke(chunks.Count);
                var translatedChunks = new List<string>(chunks.Count);
                foreach (var chunk in chunks)
                {
                    // A "thinking" model can occasionally burn its whole response on hidden reasoning and
                    // never reach the actual answer (see LanguageTools.TranslateDocumentToMarkdownAsync) -
                    // that shows up here as an empty result. One retry clears most of these; if it comes
                    // back empty twice in a row, treat it as a real failure rather than looping forever.
                    var translatedChunk = await _language.TranslateDocumentToMarkdownAsync(chunk, targetNative, sourceNative, cancellationToken: ct);
                    if (string.IsNullOrWhiteSpace(translatedChunk))
                    {
                        _logger.LogWarning(
                            "Document translation returned an empty chunk for document {DocumentId} ({ChunkIndex}/{ChunkCount}) - retrying once",
                            documentId, translatedChunks.Count + 1, chunks.Count);
                        translatedChunk = await _language.TranslateDocumentToMarkdownAsync(chunk, targetNative, sourceNative, cancellationToken: ct);
                    }

                    if (string.IsNullOrWhiteSpace(translatedChunk))
                    {
                        _logger.LogWarning(
                            "Document translation returned an empty chunk twice for document {DocumentId} ({ChunkIndex}/{ChunkCount}), giving up",
                            documentId, translatedChunks.Count + 1, chunks.Count);
                        return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Oversættelsen mislykkedes. Prøv igen senere." };
                    }
                    translatedChunks.Add(translatedChunk);
                    onProgress?.Invoke(translatedChunks.Count);
                }
                markdown = string.Join("\n\n", translatedChunks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Document translation failed for document {DocumentId}", documentId);
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Oversættelsen mislykkedes. Prøv igen senere." };
            }

            if (string.IsNullOrWhiteSpace(markdown))
                return new TranslateDocumentResponseDto { Success = false, ErrorMessage = "Oversættelsen mislykkedes. Prøv igen senere." };

            DocumentTranslation entity;
            if (existingTranslation is not null)
            {
                // Force-retry: overwrite the existing cached row in place rather than inserting a second
                // one, which the (DocumentId, LanguageCode) unique index would reject anyway.
                entity = existingTranslation;
                entity.TranslatedMarkdown = markdown;
                entity.CreatedAtUtc = DateTime.UtcNow;
            }
            else
            {
                entity = new DocumentTranslation
                {
                    DocumentId = documentId,
                    LanguageCode = targetLanguageCode,
                    TranslatedMarkdown = markdown,
                    CreatedAtUtc = DateTime.UtcNow
                };
                _context.DocumentTranslations.Add(entity);
            }

            try
            {
                await _context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                // Same race as FeedService's translation caching: two concurrent translate-clicks for the
                // same (document, language) can both miss the cache and both try to insert — the unique
                // index rejects the second insert. This request still returns its own freshly translated
                // markdown to its caller, it just doesn't also win the race to cache it.
                _context.Entry(entity).State = EntityState.Detached;
                _logger.LogDebug(ex, "Document translation cache write skipped for document {DocumentId}/{Language} (likely already cached by a concurrent request)", documentId, targetLanguageCode);
            }

            _logger.LogInformation("Document {DocumentId} translated to {Language}", documentId, targetLanguageCode);
            return new TranslateDocumentResponseDto
            {
                Success = true,
                Html = MarkdownRenderer.ToSafeHtml(markdown),
                TargetLanguageName = targetNative,
                Truncated = truncated
            };
        }

        /// <summary>
        /// Splits <paramref name="text"/> into chunks of roughly <paramref name="targetChunkChars"/>
        /// characters, breaking only between blank-line-separated blocks (paragraphs, or a whole Markdown
        /// table emitted by DocumentMarkdownExtractor) — never inside one, so a table's header/separator/
        /// rows always stay together in the same translation call. A single block larger than the target
        /// is kept whole rather than corrupted.
        /// </summary>
        private static List<string> SplitIntoChunks(string text, int targetChunkChars)
        {
            var blocks = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"\n\s*\n")
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToList();

            var chunks = new List<string>();
            var current = new System.Text.StringBuilder();

            foreach (var block in blocks)
            {
                if (current.Length > 0 && current.Length + block.Length + 2 > targetChunkChars)
                {
                    chunks.Add(current.ToString());
                    current.Clear();
                }

                if (current.Length > 0) current.Append("\n\n");
                current.Append(block);
            }

            if (current.Length > 0) chunks.Add(current.ToString());
            return chunks.Count > 0 ? chunks : new List<string> { text };
        }

        private async Task<Dictionary<string, string>> LoadDisplayNamesAsync(List<string> userIds, CancellationToken ct)
        {
            if (userIds.Count == 0) return new Dictionary<string, string>();

            return await _context.Users
                .AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => u.DisplayName, ct);
        }
    }
}
