using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data;
using web.Data.Entities;
using web.Infrastructure;
using web.Repositories.Logs.Interfaces;
using web.Services.AiGateway;
using web.Services.AiGateway.Dtos.KnowledgeBase;
using web.Services.AiGateway.Interfaces;
using web.Services.Mail.Dtos;
using web.Services.Mail.Interfaces;
using web.Services.Ollama;
using web.Services.Ollama.Dto;
using web.Services.Ollama.Interfaces;
using web.Services.Sms.Dtos.Sms;
using web.Services.Sms.Dtos.Subscriptions;
using web.Services.Sms.Interfaces;
using web.ViewModels;

namespace web.Controllers
{
    [Authorize(Policy = "AdminOrDeveloper")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogReaderService _logReaderService;
        private readonly IOllamaService _ollamaService;
        private readonly IOllamaConfigurationProvider _ollamaConfigurationProvider;
        private readonly IAiGatewayService _aiGatewayService;
        private readonly IAiGatewayConfigurationProvider _aiGatewayConfigurationProvider;
        private readonly ISmsService _smsService;
        private readonly IMailService _mailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<SettingsController> _logger;

        public SettingsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogReaderService logReaderService,
            IOllamaService ollamaService,
            IOllamaConfigurationProvider ollamaConfigurationProvider,
            IAiGatewayService aiGatewayService,
            IAiGatewayConfigurationProvider aiGatewayConfigurationProvider,
            ISmsService smsService,
            IMailService mailService,
            IConfiguration configuration,
            ILogger<SettingsController> logger)
        {
            _context = context;
            _userManager = userManager;
            _logReaderService = logReaderService;
            _ollamaService = ollamaService;
            _ollamaConfigurationProvider = ollamaConfigurationProvider;
            _aiGatewayService = aiGatewayService;
            _aiGatewayConfigurationProvider = aiGatewayConfigurationProvider;
            _smsService = smsService;
            _mailService = mailService;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            // Kun Brugere-fanen (standardfanen) indlæses med det samme. De øvrige faner
            // indlæser deres indhold via AJAX, første gang de vises (se settingsTabs-scriptet
            // i Index.cshtml), så Indstillinger-siden ikke skal vente på Ollama/SMS-gatewayen mv.
            var ollamaConfig = await _ollamaConfigurationProvider.GetActiveConfigurationAsync(HttpContext.RequestAborted);
            var aiGatewayConfig = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(HttpContext.RequestAborted);

            var model = new SettingsIndexViewModel
            {
                Users = await GetUsersAsync(new UserFilterViewModel()),
                ShowOllamaTab = !string.IsNullOrWhiteSpace(ollamaConfig.BaseUrl),
                ShowAiGatewayTab = !string.IsNullOrWhiteSpace(aiGatewayConfig.BaseUrl),
                ShowSmsTab = !string.IsNullOrWhiteSpace(_configuration["Sms:BaseUrl"])
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> UsersTabContent()
        {
            var model = await GetUsersAsync(new UserFilterViewModel());
            return PartialView("_UsersTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> RegistrationTabContent()
        {
            var model = await GetRegistrationSettingsAsync();
            return PartialView("_RegistrationTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> ThemeTabContent()
        {
            var model = await GetThemeSettingsAsync();
            return PartialView("_ThemeTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> OllamaTabContent()
        {
            var model = await GetOllamaStatusAsync(HttpContext.RequestAborted);
            return PartialView("_OllamaTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> AiGatewayTabContent()
        {
            var model = await GetAiGatewayStatusAsync(HttpContext.RequestAborted);
            return PartialView("_AiGatewayTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> SmsTabContent()
        {
            var model = await GetSmsStatusAsync(HttpContext.RequestAborted);
            return PartialView("_SmsTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> MailTabContent()
        {
            var model = await GetMailStatusAsync(HttpContext.RequestAborted);
            return PartialView("_MailTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> MailStatus()
        {
            var model = await GetMailFolderStatusAsync(HttpContext.RequestAborted);
            return PartialView("_MailStatusCards", model);
        }

        [HttpGet]
        public async Task<IActionResult> LogsTabContent(LogFilterViewModel filter)
        {
            var model = await _logReaderService.GetLogsAsync(filter, HttpContext.RequestAborted);
            return PartialView("_LogsTab", model);
        }

        [HttpGet]
        public async Task<IActionResult> UsersTable(UserFilterViewModel filter)
        {
            var model = await GetUsersAsync(filter);
            return PartialView("_UsersTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> LogsTable(LogFilterViewModel filter)
        {
            var model = await _logReaderService.GetLogsAsync(filter, HttpContext.RequestAborted);
            return PartialView("_LogsTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> OllamaTable()
        {
            var model = await GetOllamaStatusAsync(HttpContext.RequestAborted);
            return PartialView("_OllamaTabBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> OllamaRunningModelsTable()
        {
            var model = await GetOllamaRunningStatusAsync(HttpContext.RequestAborted);
            return PartialView("_OllamaRunningModelsTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> AiGatewayTable()
        {
            var model = await GetAiGatewayStatusAsync(HttpContext.RequestAborted);
            return PartialView("_AiGatewayTabBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> SmsTable(SmsFilterViewModel filter)
        {
            var model = await GetSmsMessagesAsync(filter);
            return PartialView("_SmsTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> MailTable(MailFilterViewModel filter)
        {
            var model = await GetMailMessagesAsync(filter, HttpContext.RequestAborted);
            return PartialView("_MailTableBody", model);
        }

        [HttpGet]
        public async Task<IActionResult> DownloadMailAttachment(uint uid, string fileName)
        {
            ReadMailResponseDto? message;
            try
            {
                message = await _mailService.GetMailAsync(uid, cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke forbinde til mailboksen for at hente vedhæftet fil");
                return this.ToastErrorJson("Kunne ikke forbinde til mailboksen.");
            }

            var attachment = message?.Attachments.FirstOrDefault(a => a.FileName == fileName);
            if (attachment is null)
            {
                return NotFound();
            }

            return File(attachment.Content, attachment.ContentType, attachment.FileName);
        }

        [HttpGet]
        public async Task<IActionResult> MailMessage(uint uid)
        {
            ReadMailResponseDto? message;
            try
            {
                message = await _mailService.GetMailAsync(uid, markAsSeen: true, cancellationToken: HttpContext.RequestAborted);
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke forbinde til mailboksen for at hente mail {Uid}", uid);
                return StatusCode(StatusCodes.Status502BadGateway);
            }

            if (message is null)
                return NotFound();

            return Json(new MailMessageViewModel
            {
                Uid = message.Uid,
                From = message.From,
                To = message.To,
                Subject = message.Subject,
                ReceivedAtUtc = message.ReceivedAtUtc,
                TextBody = message.TextBody,
                HtmlBody = message.HtmlBody,
                Attachments = message.Attachments
                    .Select(a => new MailAttachmentListItemViewModel { FileName = a.FileName, SizeBytes = a.Content.LongLength })
                    .ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMail(uint uid)
        {
            try
            {
                var deleted = await _mailService.DeleteMailAsync(uid, cancellationToken: HttpContext.RequestAborted);
                return deleted
                    ? this.ToastSuccessJson("Mail slettet.")
                    : this.ToastErrorJson("Mailen blev ikke fundet.");
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke slette mail {Uid}", uid);
                return this.ToastErrorJson("Kunne ikke forbinde til mailboksen.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMail(SendMailViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv mindst én modtager, et emne og en besked.");

            var recipients = model.To
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (recipients.Count == 0)
                return this.ToastErrorJson("Angiv mindst én modtager.");

            try
            {
                await _mailService.SendMailAsync(new SendMailRequestDto
                {
                    To = recipients,
                    Subject = model.Subject.Trim(),
                    TextBody = model.Body.Trim(),
                }, HttpContext.RequestAborted);

                return this.ToastSuccessJson("Mail sendt.");
            }
            catch (FormatException)
            {
                return this.ToastErrorJson("En eller flere modtageradresser er ugyldige.");
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke forbinde til mailserveren for at sende mail");
                return this.ToastErrorJson("Kunne ikke forbinde til mailserveren.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Brugeren kunne ikke oprettes. Kontroller felterne.");

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                DisplayName = model.DisplayName,
                PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber,
                IsActive = true,
                EmailConfirmed = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));

            await _userManager.AddToRoleAsync(user, model.Role);
            return this.ToastSuccessJson("Bruger oprettet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user is null)
                return this.ToastErrorJson("Brugeren blev ikke fundet.");

            if (User.Identity?.Name == user.UserName)
                return this.ToastWarningJson("Du kan ikke redigere din egen bruger her. Brug 'Min profil' i stedet.");

            if (!string.IsNullOrWhiteSpace(model.Password) && model.Password.Length < 6)
                return this.ToastErrorJson("Password skal være mindst 6 tegn.");

            user.Email = model.Email;
            user.UserName = model.Email;
            user.DisplayName = model.DisplayName;
            user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber;
            user.IsActive = model.IsActive;
            user.UpdatedAtUtc = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passwordResult = await _userManager.ResetPasswordAsync(user, token, model.Password);
                if (!passwordResult.Succeeded)
                    return this.ToastErrorJson(string.Join(" ", passwordResult.Errors.Select(e => e.Description)));
            }

            return this.ToastSuccessJson("Bruger opdateret.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
                return this.ToastErrorJson("Brugeren blev ikke fundet.");

            if (User.Identity?.Name == user.UserName)
                return this.ToastWarningJson("Du kan ikke slette din egen bruger.");

            var result = await _userManager.DeleteAsync(user);
            return result.Succeeded
                ? this.ToastSuccessJson("Bruger slettet.")
                : this.ToastErrorJson(string.Join(" ", result.Errors.Select(e => e.Description)));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRegistration(RegistrationSettingsViewModel model)
        {
            await SetSettingAsync(AppSettingKeys.AllowPublicRegistration, model.AllowPublicRegistration.ToString().ToLowerInvariant());
            return this.ToastSuccessJson("Registreringsindstillinger gemt.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateThemeSelection(ThemeSelectionViewModel model)
        {
            var lightExists = await _context.ThemeSettings.AnyAsync(t => t.ThemeMode == ThemeMode.Light && t.Name == model.ActiveThemeName);
            var darkExists = await _context.ThemeSettings.AnyAsync(t => t.ThemeMode == ThemeMode.Dark && t.Name == model.ActiveThemeName);
            if (!lightExists || !darkExists)
                return this.ToastErrorJson("Det valgte farvetema findes ikke.");

            await SetSettingAsync(AppSettingKeys.ActiveThemeMode, model.ActiveThemeMode);
            await SetSettingAsync(AppSettingKeys.ActiveThemeName, model.ActiveThemeName);

            return this.ToastSuccessJson("Tema gemt.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PullOllamaModel(PullOllamaModelViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv et gyldigt modelnavn.");

            try
            {
                var result = await _ollamaService.PullModelAsync(
                    new OllamaPullRequest { Model = model.ModelName.Trim() },
                    HttpContext.RequestAborted);

                return this.ToastSuccessJson($"Model '{model.ModelName}' hentet ({result.Status}).");
            }
            catch (OllamaException ex)
            {
                return this.ToastErrorJson($"Kunne ikke hente model: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at hente model {ModelName}", model.ModelName);
                return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteOllamaModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return this.ToastErrorJson("Modelnavn mangler.");

            try
            {
                await _ollamaService.DeleteModelAsync(
                    new OllamaDeleteRequest { Model = modelName },
                    HttpContext.RequestAborted);

                return this.ToastSuccessJson($"Model '{modelName}' slettet.");
            }
            catch (OllamaException ex)
            {
                return this.ToastErrorJson($"Kunne ikke slette model: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at slette model {ModelName}", modelName);
                return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoadOllamaModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return this.ToastErrorJson("Modelnavn mangler.");

            try
            {
                await _ollamaService.GenerateAsync(
                    new OllamaGenerateRequest { Model = modelName },
                    HttpContext.RequestAborted);

                return this.ToastSuccessJson($"Model '{modelName}' indlæst i hukommelsen.");
            }
            catch (OllamaException ex)
            {
                return this.ToastErrorJson($"Kunne ikke indlæse model: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at indlæse model {ModelName}", modelName);
                return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnloadOllamaModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName))
                return this.ToastErrorJson("Modelnavn mangler.");

            try
            {
                await _ollamaService.GenerateAsync(
                    new OllamaGenerateRequest { Model = modelName, KeepAlive = JsonSerializer.SerializeToElement(0) },
                    HttpContext.RequestAborted);

                return this.ToastSuccessJson($"Model '{modelName}' fjernet fra hukommelsen.");
            }
            catch (OllamaException ex)
            {
                return this.ToastErrorJson($"Kunne ikke fjerne model: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte Ollama-serveren for at fjerne model {ModelName}", modelName);
                return this.ToastErrorJson("Kunne ikke kontakte Ollama-serveren.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAiGatewayGroup(CreateAiGatewayGroupViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv et gyldigt gruppenavn.");

            try
            {
                await _aiGatewayService.CreateGroupAsync(new CreateGroupRequestDto { Name = model.Name.Trim() }, HttpContext.RequestAborted);
                return this.ToastSuccessJson($"Gruppen '{model.Name}' er oprettet.");
            }
            catch (AiGatewayException ex)
            {
                return this.ToastErrorJson($"Kunne ikke oprette gruppe: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at oprette gruppen {GroupName}", model.Name);
                return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAiGatewayGroup(Guid id)
        {
            try
            {
                await _aiGatewayService.DeleteGroupAsync(id, HttpContext.RequestAborted);
                return this.ToastSuccessJson("Gruppen er slettet.");
            }
            catch (AiGatewayException ex)
            {
                return this.ToastErrorJson($"Kunne ikke slette gruppe: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at slette gruppen {GroupId}", id);
                return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAiGatewayDocument(UploadAiGatewayDocumentViewModel model)
        {
            if (!ModelState.IsValid || model.File is null || model.File.Length == 0)
                return this.ToastErrorJson("Vælg en gyldig fil og gruppe.");

            try
            {
                await using var stream = model.File.OpenReadStream();
                await _aiGatewayService.UploadDocumentAsync(new UploadDocumentRequestDto
                {
                    GroupId = model.GroupId,
                    FileContent = stream,
                    FileName = model.File.FileName,
                    ContentType = model.File.ContentType
                }, HttpContext.RequestAborted);

                return this.ToastSuccessJson($"Dokumentet '{model.File.FileName}' er uploadet.");
            }
            catch (AiGatewayException ex)
            {
                return this.ToastErrorJson($"Kunne ikke uploade dokument: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at uploade dokumentet {FileName}", model.File?.FileName);
                return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAiGatewayDocument(Guid id)
        {
            try
            {
                await _aiGatewayService.DeleteDocumentAsync(id, HttpContext.RequestAborted);
                return this.ToastSuccessJson("Dokumentet er slettet.");
            }
            catch (AiGatewayException ex)
            {
                return this.ToastErrorJson($"Kunne ikke slette dokument: {ex.Message}");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke kontakte AiGateway for at slette dokumentet {DocumentId}", id);
                return this.ToastErrorJson("Kunne ikke kontakte AiGateway.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> DownloadAiGatewayDocument(Guid id)
        {
            try
            {
                var result = await _aiGatewayService.DownloadDocumentAsync(id, HttpContext.RequestAborted);
                return File(result.Content, result.ContentType, result.FileName ?? "document");
            }
            catch (AiGatewayException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound();
            }
            catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente dokumentet {DocumentId} fra AiGateway", id);
                return StatusCode(StatusCodes.Status502BadGateway);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendSms(SendSmsViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv et gyldigt telefonnummer og en besked.");

            _context.SmsMessages.Add(new SmsMessage
            {
                PhoneNumber = model.To.Trim(),
                Body = model.Message.Trim()
            });

            await _context.SaveChangesAsync();
            return this.ToastSuccessJson("SMS lagt i kø til afsendelse.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSms(int id)
        {
            var message = await _context.SmsMessages.FindAsync(id);
            if (message is null)
                return this.ToastErrorJson("Sms'en blev ikke fundet.");

            if (message.Status != SmsMessageStatus.Queued && message.Status != SmsMessageStatus.Failed)
                return this.ToastErrorJson("Kun sms'er med status Queued eller Failed kan slettes.");

            if (message.Status == SmsMessageStatus.Queued && message.GatewayMessageId.HasValue)
            {
                try
                {
                    await _smsService.DeleteSmsAsync(message.GatewayMessageId.Value, cancellationToken: HttpContext.RequestAborted);
                }
                catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                {
                    _logger.LogWarning(ex, "Kunne ikke annullere sms {SmsMessageId} (gateway id {GatewayMessageId}) i SMS-gatewayen", message.Id, message.GatewayMessageId);
                    return this.ToastErrorJson("Kunne ikke annullere sms'en i SMS-gatewayen. Den er måske allerede sendt.");
                }
            }

            _context.SmsMessages.Remove(message);
            await _context.SaveChangesAsync();
            return this.ToastSuccessJson("Sms slettet.");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetrySms(int id)
        {
            var message = await _context.SmsMessages.FindAsync(id);
            if (message is null)
                return this.ToastErrorJson("Sms'en blev ikke fundet.");

            if (message.Direction != SmsDirection.Outbound || message.Status != SmsMessageStatus.Failed)
                return this.ToastErrorJson("Kun sms'er med status Failed kan sendes igen.");

            message.GatewayMessageId = null;
            message.Status = SmsMessageStatus.Pending;
            message.FailureReason = null;
            message.FailedAtUtc = null;
            message.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return this.ToastSuccessJson("Sms lagt i kø til afsendelse igen.");
        }

        [HttpGet]
        public async Task<IActionResult> SmsSubscriberNumbers()
        {
            try
            {
                var subscriptions = await _smsService.GetAllSubscriptionsAsync(cancellationToken: HttpContext.RequestAborted);

                return Json(new SmsSubscriberNumbersViewModel
                {
                    Subscriptions = subscriptions
                        .Select(s => new SmsSubscriptionViewModel
                        {
                            SubscriptionId = s.Id,
                            PhoneNumbers = s.PhoneNumbers,
                            StartDate = s.StartDate,
                            EndDate = s.EndDate,
                            WebhookUrl = s.WebhookUrl,
                            IsActive = s.IsActive
                        })
                        .OrderByDescending(s => s.IsActive)
                        .ThenByDescending(s => s.StartDate)
                        .ToList()
                });
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente modtagernumre fra SMS-gatewayen");
                return StatusCode(StatusCodes.Status502BadGateway);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSmsSubscriberNumbers(SaveSmsSubscriberNumbersViewModel model)
        {
            if (!ModelState.IsValid)
                return this.ToastErrorJson("Angiv en gyldig startdato og slutdato.");

            if (model.EndDate < model.StartDate)
                return this.ToastErrorJson("Slutdato skal være efter startdato.");

            var phoneNumbers = model.PhoneNumbers
                .Select(n => n.Trim())
                .Where(n => n.Length > 0)
                .Distinct()
                .ToList();

            try
            {
                if (model.SubscriptionId.HasValue && phoneNumbers.Count == 0)
                {
                    await _smsService.DeleteSubscriptionAsync(model.SubscriptionId.Value, cancellationToken: HttpContext.RequestAborted);
                    return this.ToastSuccessJson("Alle modtagernumre er fjernet.");
                }

                if (phoneNumbers.Count == 0)
                    return this.ToastErrorJson("Tilføj mindst ét nummer.");

                var webhookUrl = string.IsNullOrWhiteSpace(model.WebhookUrl) ? null : model.WebhookUrl.Trim();

                if (model.SubscriptionId.HasValue)
                {
                    await _smsService.UpdateSubscriptionAsync(model.SubscriptionId.Value, new UpdateSubscriptionsRequestDto
                    {
                        PhoneNumbers = phoneNumbers,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        WebhookUrl = webhookUrl
                    }, cancellationToken: HttpContext.RequestAborted);
                }
                else
                {
                    await _smsService.CreateSubscriptionAsync(new CreateSubscriptionsRequestDto
                    {
                        PhoneNumbers = phoneNumbers,
                        StartDate = model.StartDate,
                        EndDate = model.EndDate,
                        WebhookUrl = webhookUrl
                    }, cancellationToken: HttpContext.RequestAborted);
                }

                return this.ToastSuccessJson("Modtagernumre gemt.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Kunne ikke gemme modtagernumre i SMS-gatewayen");
                var message = ex.StatusCode switch
                {
                    System.Net.HttpStatusCode.Conflict => "Et eller flere numre er allerede registreret til en anden nøgle i SMS-gatewayen. Fjern dem fra listen og forsøg igen.",
                    System.Net.HttpStatusCode.BadRequest => "SMS-gatewayen afviste numrene. Kontroller at de er angivet korrekt (f.eks. med landekode).",
                    _ => "Kunne ikke gemme modtagernumre i SMS-gatewayen."
                };
                return this.ToastErrorJson(message);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, "Tidsudløb ved kontakt til SMS-gatewayen for at gemme modtagernumre");
                return this.ToastErrorJson("Kunne ikke forbinde til SMS-gatewayen.");
            }
        }

        private async Task<RegistrationSettingsViewModel> GetRegistrationSettingsAsync()
        {
            return new RegistrationSettingsViewModel
            {
                AllowPublicRegistration = await GetBoolSettingAsync(AppSettingKeys.AllowPublicRegistration)
            };
        }

        private async Task<ThemeSettingsViewModel> GetThemeSettingsAsync()
        {
            return new ThemeSettingsViewModel
            {
                ActiveThemeMode = await GetSettingAsync(AppSettingKeys.ActiveThemeMode, ThemeMode.System),
                ActiveThemeName = await GetSettingAsync(AppSettingKeys.ActiveThemeName, "Orange"),
                Themes = await _context.ThemeSettings.AsNoTracking().OrderBy(t => t.ThemeMode).ToListAsync()
            };
        }

        private async Task<UserFilterViewModel> GetUsersAsync(UserFilterViewModel filter)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;
            filter.CurrentUserId = _userManager.GetUserId(User);

            var users = await _context.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync();
            var items = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                items.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? string.Empty,
                    DisplayName = user.DisplayName,
                    PhoneNumber = user.PhoneNumber,
                    IsActive = user.IsActive,
                    CreatedAtUtc = user.CreatedAtUtc,
                    Roles = roles.ToList()
                });
            }

            var query = items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(u =>
                    u.DisplayName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(filter.Role))
            {
                query = query.Where(u => u.Roles.Contains(filter.Role));
            }

            if (filter.IsActive.HasValue)
            {
                query = query.Where(u => u.IsActive == filter.IsActive.Value);
            }

            var filtered = query.ToList();
            filter.TotalCount = filtered.Count;
            filter.Users = filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            return filter;
        }

        private async Task<OllamaSettingsViewModel> GetOllamaRunningStatusAsync(CancellationToken cancellationToken)
        {
            var config = await _ollamaConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
            var model = new OllamaSettingsViewModel
            {
                BaseUrl = config.BaseUrl,
                DefaultChatModel = config.DefaultChatModel
            };

            try
            {
                var version = await _ollamaService.GetVersionAsync(cancellationToken);
                model.Version = version.Version;
                model.IsOnline = true;

                var running = await _ollamaService.ListRunningModelsAsync(cancellationToken);
                model.RunningModels = running.Models
                    .Select(m => new OllamaRunningModelViewModel
                    {
                        Name = m.Name,
                        SizeVram = m.SizeVram ?? m.Size,
                        ExpiresAt = m.ExpiresAt
                    })
                    .ToList();
            }
            catch (Exception ex) when (ex is OllamaException or HttpRequestException or TaskCanceledException)
            {
                model.IsOnline = false;
                model.ErrorMessage = "Kunne ikke forbinde til Ollama-serveren.";
                _logger.LogWarning(ex, "Ollama-serveren på {BaseUrl} svarede ikke", config.BaseUrl);
            }

            return model;
        }

        private async Task<OllamaSettingsViewModel> GetOllamaStatusAsync(CancellationToken cancellationToken)
        {
            var model = await GetOllamaRunningStatusAsync(cancellationToken);
            if (!model.IsOnline)
                return model;

            try
            {
                var tags = await _ollamaService.ListModelsAsync(cancellationToken);
                var runningNames = model.RunningModels.Select(r => r.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
                model.InstalledModels = tags.Models
                    .Select(m => new OllamaInstalledModelViewModel
                    {
                        Name = m.Name,
                        Size = m.Size,
                        ParameterSize = m.Details?.ParameterSize,
                        QuantizationLevel = m.Details?.QuantizationLevel,
                        Family = m.Details?.Family,
                        ModifiedAt = m.ModifiedAt,
                        IsRunning = runningNames.Contains(m.Name)
                    })
                    .OrderBy(m => m.Name)
                    .ToList();
            }
            catch (Exception ex) when (ex is OllamaException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente modeloversigt fra Ollama-serveren på {BaseUrl}", model.BaseUrl);
            }

            return model;
        }

        private async Task<AiGatewaySettingsViewModel> GetAiGatewayStatusAsync(CancellationToken cancellationToken)
        {
            var config = await _aiGatewayConfigurationProvider.GetActiveConfigurationAsync(cancellationToken);
            var model = new AiGatewaySettingsViewModel { BaseUrl = config.BaseUrl, DefaultChatModel = config.DefaultChatModel };

            try
            {
                var health = await _aiGatewayService.CheckHealthAsync(cancellationToken);
                model.IsOnline = true;
                model.Status = health.Status;
                model.HealthChecks = (health.Checks ?? [])
                    .Select(c => new AiGatewayHealthCheckViewModel { Name = c.Name, Status = c.Status, Description = c.Description })
                    .ToList();
            }
            catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
            {
                model.IsOnline = false;
                model.ErrorMessage = "Kunne ikke forbinde til AiGateway.";
                _logger.LogWarning(ex, "AiGateway på {BaseUrl} svarede ikke", config.BaseUrl);
                return model;
            }

            try
            {
                var groups = (await _aiGatewayService.ListGroupsAsync(cancellationToken)).Groups ?? [];
                var documents = (await _aiGatewayService.ListDocumentsAsync(null, cancellationToken)).Documents ?? [];

                var groupNameById = groups.ToDictionary(g => g.Id, g => g.Name ?? string.Empty);
                var documentCountByGroup = documents
                    .GroupBy(d => d.GroupId)
                    .ToDictionary(g => g.Key, g => g.Count());

                model.Groups = groups
                    .Select(g => new AiGatewayGroupViewModel
                    {
                        Id = g.Id,
                        Name = g.Name ?? string.Empty,
                        CreatedAtUtc = g.CreatedAt,
                        DocumentCount = documentCountByGroup.GetValueOrDefault(g.Id)
                    })
                    .OrderBy(g => g.Name)
                    .ToList();

                model.Documents = documents
                    .Select(d => new AiGatewayDocumentViewModel
                    {
                        Id = d.Id,
                        GroupId = d.GroupId,
                        GroupName = groupNameById.GetValueOrDefault(d.GroupId),
                        FileName = d.FileName ?? string.Empty,
                        ContentType = d.ContentType,
                        SizeBytes = d.SizeBytes,
                        Status = d.Status,
                        UploadedAtUtc = d.UploadedAt
                    })
                    .OrderByDescending(d => d.UploadedAtUtc)
                    .ToList();
            }
            catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente KnowledgeBase-data fra AiGateway på {BaseUrl}", config.BaseUrl);
            }

            try
            {
                var installed = (await _aiGatewayService.OllamaListModelsAsync(cancellationToken)).Models ?? [];
                var running = (await _aiGatewayService.OllamaListRunningModelsAsync(cancellationToken)).Models ?? [];
                var runningNames = running
                    .Where(r => r.Name is not null)
                    .Select(r => r.Name!)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                model.OllamaModels = installed
                    .Select(m => new AiGatewayOllamaModelViewModel
                    {
                        Name = m.Name ?? string.Empty,
                        Size = m.Size,
                        ParameterSize = m.Details?.ParameterSize,
                        QuantizationLevel = m.Details?.QuantizationLevel,
                        Family = m.Details?.Family,
                        ModifiedAt = m.ModifiedAt,
                        IsRunning = m.Name is not null && runningNames.Contains(m.Name)
                    })
                    .OrderBy(m => m.Name)
                    .ToList();
            }
            catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente Ollama-modeloversigt fra AiGateway på {BaseUrl}", config.BaseUrl);
            }

            try
            {
                var models = (await _aiGatewayService.SpeachesListAudioModelsAsync(cancellationToken)).Models ?? [];
                var running = (await _aiGatewayService.SpeachesListRunningModelsAsync(cancellationToken)).Models ?? [];
                var runningIds = running.ToHashSet(StringComparer.OrdinalIgnoreCase);

                model.SpeachesModels = models
                    .Select(m => new AiGatewaySpeachesModelViewModel
                    {
                        Id = m.Id ?? string.Empty,
                        OwnedBy = m.OwnedBy,
                        Task = m.Task,
                        Language = m.Language ?? [],
                        IsRunning = m.Id is not null && runningIds.Contains(m.Id)
                    })
                    .OrderBy(m => m.Id)
                    .ToList();
            }
            catch (Exception ex) when (ex is AiGatewayException or HttpRequestException or TaskCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente Speaches-modeloversigt fra AiGateway på {BaseUrl}", config.BaseUrl);
            }

            return model;
        }

        private async Task<SmsSettingsViewModel> GetSmsStatusAsync(CancellationToken cancellationToken)
        {
            var model = new SmsSettingsViewModel
            {
                Messages = await GetSmsMessagesAsync(new SmsFilterViewModel())
            };

            try
            {
                var balance = await _smsService.GetBalanceCostAsync(cancellationToken: cancellationToken);
                if (balance is not null)
                {
                    model.BalanceDkk = balance.Balance;
                    model.BalanceUpdatedAtUtc = balance.UpdatedAt;
                }

                var currentCost = await _smsService.GetCurrentCostAsync(cancellationToken: cancellationToken);
                if (currentCost is not null)
                {
                    model.CurrentPriceDkk = currentCost.SmsPriceDkk;
                    model.CurrentPriceUpdatedAtUtc = currentCost.UpdatedAt;
                }

                model.IsOnline = true;
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
            {
                model.IsOnline = false;
                model.ErrorMessage = "Kunne ikke forbinde til SMS-gatewayen.";
                _logger.LogWarning(ex, "SMS-gatewayen svarede ikke");
            }

            return model;
        }

        private async Task<SmsFilterViewModel> GetSmsMessagesAsync(SmsFilterViewModel filter)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;

            var query = _context.SmsMessages.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Direction))
            {
                query = query.Where(m => m.Direction == filter.Direction);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(m =>
                    m.PhoneNumber.Contains(term) ||
                    m.Body.Contains(term) ||
                    m.Status.Contains(term));
            }

            filter.TotalCount = await query.CountAsync();
            filter.Items = await query
                .OrderByDescending(m => m.CreatedAtUtc)
                .Skip((filter.Page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .Select(m => new SmsListItemViewModel
                {
                    Id = m.Id,
                    GatewayMessageId = m.GatewayMessageId,
                    Direction = m.Direction,
                    PhoneNumber = m.PhoneNumber,
                    Body = m.Body,
                    Status = m.Status,
                    SegmentCount = m.SegmentCount,
                    TotalPriceDkk = m.TotalPriceDkk,
                    FailureReason = m.FailureReason,
                    CreatedAtUtc = m.CreatedAtUtc
                })
                .ToListAsync();

            return filter;
        }

        private async Task<MailSettingsViewModel> GetMailStatusAsync(CancellationToken cancellationToken)
        {
            var model = await GetMailFolderStatusAsync(cancellationToken);
            model.Messages = await GetMailMessagesAsync(new MailFilterViewModel(), cancellationToken);
            return model;
        }

        private async Task<MailSettingsViewModel> GetMailFolderStatusAsync(CancellationToken cancellationToken)
        {
            var model = new MailSettingsViewModel();

            try
            {
                var status = await _mailService.GetFolderStatusAsync(cancellationToken: cancellationToken);
                model.TotalMessageCount = status.TotalCount;
                model.UnseenMessageCount = status.UnseenCount;
                model.IsOnline = true;
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                model.IsOnline = false;
                model.ErrorMessage = "Kunne ikke forbinde til mailboksen.";
                _logger.LogWarning(ex, "Mailboksen svarede ikke");
            }

            return model;
        }

        private async Task<MailFilterViewModel> GetMailMessagesAsync(MailFilterViewModel filter, CancellationToken cancellationToken)
        {
            filter.Page = filter.Page < 1 ? 1 : filter.Page;
            filter.PageSize = filter.PageSize is < 5 or > 200 ? 10 : filter.PageSize;

            List<MailListItemViewModel> items;
            try
            {
                var messages = await _mailService.ReadMailAsync(
                    new ReadMailRequestDto { Folder = "INBOX", MaxCount = 50 },
                    cancellationToken);

                items = messages
                    .Select(m => new MailListItemViewModel
                    {
                        Uid = m.Uid,
                        From = m.From,
                        Subject = m.Subject,
                        ReceivedAtUtc = m.ReceivedAtUtc,
                        IsSeen = m.IsSeen,
                        Attachments = m.Attachments
                            .Select(a => new MailAttachmentListItemViewModel { FileName = a.FileName, SizeBytes = a.Content.LongLength })
                            .ToList()
                    })
                    .OrderByDescending(m => m.ReceivedAtUtc)
                    .ToList();
            }
            catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException or OperationCanceledException)
            {
                _logger.LogWarning(ex, "Kunne ikke hente mails fra indbakken");
                items = [];
            }

            var query = items.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(filter.SearchText))
            {
                var term = filter.SearchText.Trim();
                query = query.Where(m =>
                    m.From.Contains(term, StringComparison.OrdinalIgnoreCase) ||
                    m.Subject.Contains(term, StringComparison.OrdinalIgnoreCase));
            }

            var filtered = query.ToList();
            filter.TotalCount = filtered.Count;
            filter.Items = filtered.Skip((filter.Page - 1) * filter.PageSize).Take(filter.PageSize).ToList();

            return filter;
        }

        private async Task<string> GetSettingAsync(string key, string defaultValue)
        {
            var setting = await _context.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Key == key);
            return setting?.Value ?? defaultValue;
        }

        private async Task<bool> GetBoolSettingAsync(string key)
        {
            var value = await GetSettingAsync(key, "false");
            return bool.TryParse(value, out var result) && result;
        }

        private async Task SetSettingAsync(string key, string value)
        {
            var setting = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key);
            if (setting is null)
            {
                _context.AppSettings.Add(new AppSetting
                {
                    Key = key,
                    Value = value,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                setting.Value = value;
                setting.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
