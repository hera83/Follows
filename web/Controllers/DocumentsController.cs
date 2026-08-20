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

        public DocumentsController(
            UserManager<ApplicationUser> userManager,
            IDocumentsService documentsService)
        {
            _userManager = userManager;
            _documentsService = documentsService;
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
        /// Extracts the document's text and translates it to the caller's preferred language (from their
        /// profile), formatted as Markdown and rendered to HTML — consumed by the preview modal's
        /// "Oversæt"/"Genoversæt"-button. Returns success:false with a toast-style message on failure.
        /// A cached translation is normally reused (see DocumentsService); pass <paramref name="force"/> to
        /// skip that cache and translate again from scratch, overwriting it.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Translate(int id, bool force = false)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user is null) return Unauthorized();

            var result = await _documentsService.TranslateDocumentAsync(id, user.PreferredLanguage, force, HttpContext.RequestAborted);
            if (!result.Success)
                return this.ToastErrorJson(result.ErrorMessage ?? "Oversættelsen mislykkedes.");

            return Json(new
            {
                success = true,
                alreadyInTargetLanguage = result.AlreadyInTargetLanguage,
                targetLanguageName = result.TargetLanguageName,
                html = result.Html,
                truncated = result.Truncated
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
