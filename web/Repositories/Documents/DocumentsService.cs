using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Repositories.Documents.Dtos;
using web.Repositories.Documents.Interfaces;

namespace web.Repositories.Documents
{
    public class DocumentsService : IDocumentsService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;
        private readonly ILogger<DocumentsService> _logger;

        public DocumentsService(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IConfiguration config,
            ILogger<DocumentsService> logger)
        {
            _context = context;
            _env = env;
            _config = config;
            _logger = logger;
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
