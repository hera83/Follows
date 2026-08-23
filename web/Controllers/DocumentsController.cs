using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using web.Constants;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Documents.Dtos;
using web.Repositories.Documents.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize]
    public class DocumentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IDocumentsService _documentsService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly DocumentTranslationJobTracker _translationJobs;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            UserManager<ApplicationUser> userManager,
            IDocumentsService documentsService,
            IServiceScopeFactory scopeFactory,
            DocumentTranslationJobTracker translationJobs,
            ILogger<DocumentsController> logger)
        {
            _userManager = userManager;
            _documentsService = documentsService;
            _scopeFactory = scopeFactory;
            _translationJobs = translationJobs;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var groups = await _documentsService.GetGroupsAsync(HttpContext.RequestAborted);
            return View(new DocumentsIndexViewModel { Groups = groups.Select(MapGroupToViewModel).ToList() });
        }

        [HttpGet]
        public async Task<IActionResult> GroupsList()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var groups = await _documentsService.GetGroupsAsync(HttpContext.RequestAborted);
            return PartialView("_DocumentGroupsList", new DocumentsIndexViewModel { Groups = groups.Select(MapGroupToViewModel).ToList() });
        }

        [HttpGet]
        public async Task<IActionResult> Group(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return RedirectToAction("Login", "Account");

            var detail = await _documentsService.GetGroupDetailAsync(id, user.Id, IsModerator(), HttpContext.RequestAborted);
            if (detail is null) return NotFound();

            return View(MapDetailToViewModel(detail));
        }

        [HttpGet]
        public async Task<IActionResult> DocumentsList(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var detail = await _documentsService.GetGroupDetailAsync(groupId, user.Id, IsModerator(), HttpContext.RequestAborted);
            if (detail is null) return NotFound();

            return PartialView("_DocumentsList", MapDetailToViewModel(detail));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup(CreateDocumentGroupViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Gruppen kunne ikke oprettes." : errors);
            }

            var result = await _documentsService.CreateGroupAsync(new CreateDocumentGroupRequestDto
            {
                Name = model.Name,
                Description = model.Description,
                CreatedByUserId = user.Id
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke oprette gruppen.");

            return Json(new { success = true, message = $"Gruppen '{model.Name}' er oprettet.", type = "success", groupId = result.GroupId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGroup(EditDocumentGroupViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            if (!ModelState.IsValid)
            {
                var errors = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                return this.ToastErrorJson(string.IsNullOrWhiteSpace(errors) ? "Gruppen kunne ikke opdateres." : errors);
            }

            var result = await _documentsService.UpdateGroupAsync(new UpdateDocumentGroupRequestDto
            {
                GroupId = model.GroupId,
                Name = model.Name,
                Description = model.Description,
                RequestingUserId = user.Id,
                IsModerator = IsModerator()
            }, HttpContext.RequestAborted);

            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke opdatere gruppen.");

            return Json(new { success = true, message = "Gruppen er opdateret.", type = "success", name = model.Name, description = model.Description });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGroup(int groupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _documentsService.DeleteGroupAsync(groupId, user.Id, IsModerator(), HttpContext.RequestAborted);

            return result.Success
                ? this.ToastSuccessJson("Gruppen er slettet.")
                : this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke slette gruppen.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(DocumentLimits.MaxFileBytes * DocumentLimits.MaxFilesPerUpload + 5_000_000)]
        public async Task<IActionResult> Upload(int groupId, List<IFormFile> files)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            files ??= new List<IFormFile>();
            files = files.Where(f => f.Length > 0).ToList();

            if (files.Count == 0)
                return this.ToastErrorJson("Vælg mindst én fil.");

            if (files.Count > DocumentLimits.MaxFilesPerUpload)
                return this.ToastErrorJson($"Du kan højst uploade {DocumentLimits.MaxFilesPerUpload} filer ad gangen.");

            foreach (var file in files)
            {
                if (!DocumentLimits.AllowedContentTypes.Contains(file.ContentType))
                    return this.ToastErrorJson($"Filtypen for '{file.FileName}' er ikke understøttet.");

                if (file.Length > DocumentLimits.MaxFileBytes)
                    return this.ToastErrorJson($"'{file.FileName}' er for stor (max {DocumentLimits.MaxFileBytes / 1024 / 1024} MB pr. fil).");
            }

            var fileInputs = new List<DocumentFileInputDto>();
            var streams = new List<Stream>();
            try
            {
                foreach (var file in files)
                {
                    var stream = file.OpenReadStream();
                    streams.Add(stream);
                    fileInputs.Add(new DocumentFileInputDto
                    {
                        Content = stream,
                        ContentType = file.ContentType,
                        OriginalFileName = file.FileName
                    });
                }

                var result = await _documentsService.UploadDocumentsAsync(new UploadDocumentsRequestDto
                {
                    GroupId = groupId,
                    UploadedByUserId = user.Id,
                    Files = fileInputs
                }, HttpContext.RequestAborted);

                if (!result.Success)
                    return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke uploade filerne.");

                return Json(new { success = true, message = $"{result.UploadedCount} fil(er) uploadet.", type = "success" });
            }
            finally
            {
                foreach (var s in streams) await s.DisposeAsync();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDocument(int documentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _documentsService.DeleteDocumentAsync(documentId, user.Id, IsModerator(), HttpContext.RequestAborted);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Kunne ikke slette dokumentet.");

            return Json(new { success = true, message = "Dokumentet er slettet.", type = "success", groupId = result.GroupId });
        }

        /// <summary>Serves the file inline (no Content-Disposition filename) for use in the preview modal's iframe/img/text fetch.</summary>
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var file = await _documentsService.GetDocumentFileAsync(id, HttpContext.RequestAborted);
            if (file is null) return NotFound();

            return PhysicalFile(file.FullPath, file.ContentType, enableRangeProcessing: true);
        }

        /// <summary>Same file as View/{id}, but forces a download via Content-Disposition: attachment.</summary>
        [HttpGet]
        public async Task<IActionResult> Download(int id)
        {
            var file = await _documentsService.GetDocumentFileAsync(id, HttpContext.RequestAborted);
            if (file is null) return NotFound();

            return PhysicalFile(file.FullPath, file.ContentType, file.OriginalFileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Kicks off text extraction + translation of the document to the caller's preferred language (from
        /// their profile) as a detached background task, and returns a job id immediately — consumed by the
        /// preview modal's "Oversæt"/"Genoversæt"-button, which then polls <see cref="TranslateStatus"/> for
        /// "X af Y chunks" progress instead of holding one HTTP request open for the whole translation. A
        /// long document translated chunk-by-chunk in a single request used to trip proxy/gateway timeouts
        /// long before the AI Gateway itself was actually stuck; this sidesteps that entirely, since no
        /// single request needs to stay open longer than a poll interval. A cached translation is normally
        /// reused (see DocumentsService); pass <paramref name="force"/> to skip that cache and translate
        /// again from scratch, overwriting it.
        ///
        /// Runs in its own DI scope (via <see cref="IServiceScopeFactory"/>) rather than reusing this
        /// request's scoped services — the scope (and its ApplicationDbContext) would otherwise be disposed
        /// as soon as this action returns, well before the background work is done.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TranslateStart(int id, bool force = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var jobId = _translationJobs.Start(user.Id);
            var preferredLanguage = user.PreferredLanguage;

            _ = Task.Run(async () =>
            {
                using var scope = _scopeFactory.CreateScope();
                var documentsService = scope.ServiceProvider.GetRequiredService<IDocumentsService>();
                try
                {
                    var result = await documentsService.TranslateDocumentAsync(
                        id,
                        preferredLanguage,
                        force,
                        onChunkCountKnown: total => _translationJobs.ReportChunkCount(jobId, total),
                        onProgress: completed => _translationJobs.ReportProgress(jobId, completed));

                    if (!result.Success)
                    {
                        _translationJobs.Fail(jobId, result.ErrorMessage ?? "Oversættelsen mislykkedes.");
                        return;
                    }

                    _translationJobs.Complete(jobId, job =>
                    {
                        job.Success = true;
                        job.AlreadyInTargetLanguage = result.AlreadyInTargetLanguage;
                        job.TargetLanguageName = result.TargetLanguageName;
                        job.Html = result.Html;
                        job.Truncated = result.Truncated;
                    });
                }
                catch (Exception ex)
                {
                    // Not expected to happen — TranslateDocumentAsync catches its own failures and returns
                    // Success:false instead of throwing. Caught here anyway so a genuinely unexpected bug
                    // doesn't leave the job stuck at "running" forever with the client polling indefinitely.
                    _logger.LogError(ex, "Unexpected failure in background translation of document {DocumentId}", id);
                    _translationJobs.Fail(jobId, "Oversættelsen mislykkedes. Prøv igen senere.");
                }
            });

            return Json(new { jobId });
        }

        /// <summary>Polled by the preview modal after <see cref="TranslateStart"/> to show progress and, once done, the result.</summary>
        [HttpGet]
        public async Task<IActionResult> TranslateStatus(string jobId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var job = _translationJobs.Get(jobId, user.Id);
            if (job is null) return NotFound();

            if (job.Status == DocumentTranslationJobStatus.Running)
                return Json(new { status = "running", completed = job.CompletedChunks, total = job.TotalChunks });

            // Terminal state - the client has (or is about to) read the result, no reason to keep it around.
            _translationJobs.Remove(jobId);

            if (job.Status == DocumentTranslationJobStatus.Failed)
                return Json(new { status = "failed", message = job.ErrorMessage ?? "Oversættelsen mislykkedes." });

            return Json(new
            {
                status = "completed",
                alreadyInTargetLanguage = job.AlreadyInTargetLanguage,
                targetLanguageName = job.TargetLanguageName,
                html = job.Html,
                truncated = job.Truncated
            });
        }

        private bool IsModerator() =>
            User.IsInRole(AppRoles.Administrator) || User.IsInRole(AppRoles.Developer);

        private static DocumentGroupViewModel MapGroupToViewModel(DocumentGroupDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            DocumentCount = dto.DocumentCount,
            CreatedAtUtc = dto.CreatedAtUtc
        };

        private static DocumentGroupDetailViewModel MapDetailToViewModel(DocumentGroupDetailDto dto) => new()
        {
            Id = dto.Id,
            Name = dto.Name,
            Description = dto.Description,
            CanManage = dto.CanManage,
            Documents = dto.Documents.Select(d => new DocumentViewModel
            {
                Id = d.Id,
                GroupId = d.GroupId,
                Title = d.Title,
                ContentType = d.ContentType,
                FileSizeBytes = d.FileSizeBytes,
                UploadedByDisplayName = d.UploadedByDisplayName,
                CreatedAtUtc = d.CreatedAtUtc,
                CanPreviewInline = d.CanPreviewInline,
                CanTranslate = d.CanTranslate,
                CanDelete = d.CanDelete
            }).ToList()
        };
    }
}
