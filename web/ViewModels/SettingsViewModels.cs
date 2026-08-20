using System.ComponentModel.DataAnnotations;
using web.Constants;
using web.Data.Entities;

namespace web.ViewModels
{
    public class SettingsIndexViewModel
    {
        public UserFilterViewModel Users { get; set; } = new();
        public RegistrationSettingsViewModel Registration { get; set; } = new();
        public ThemeSettingsViewModel Theme { get; set; } = new();
        public OllamaSettingsViewModel Ollama { get; set; } = new();
        public AiGatewaySettingsViewModel AiGateway { get; set; } = new();
        public SmsSettingsViewModel Sms { get; set; } = new();
        public MailSettingsViewModel Mail { get; set; } = new();
        public LogFilterViewModel Logs { get; set; } = new();

        public bool ShowOllamaTab { get; set; }
        public bool ShowAiGatewayTab { get; set; }
        public bool ShowSmsTab { get; set; }
    }

    public class UserListItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = new();
        public DateTime CreatedAtUtc { get; set; }
    }

    public class UserFilterViewModel
    {
        public string? SearchText { get; set; }
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<UserListItemViewModel> Users { get; set; } = new();
        public int TotalCount { get; set; }
        public string? CurrentUserId { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Email er påkrævet")]
        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Visningsnavn er påkrævet")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Password er påkrævet")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = AppRoles.User;
    }

    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email er påkrævet")]
        [EmailAddress(ErrorMessage = "Ugyldig email-adresse")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Visningsnavn er påkrævet")]
        [StringLength(100)]
        public string DisplayName { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }

        [Required]
        public string Role { get; set; } = AppRoles.User;

        [StringLength(100, ErrorMessage = "Password må ikke være længere end 100 tegn")]
        [DataType(DataType.Password)]
        public string? Password { get; set; }
    }

    public class RegistrationSettingsViewModel
    {
        public bool AllowPublicRegistration { get; set; }
    }

    public class ThemeSettingsViewModel
    {
        public string ActiveThemeMode { get; set; } = ThemeMode.System;
        public string ActiveThemeName { get; set; } = "Orange";
        public List<ThemeSetting> Themes { get; set; } = new();
    }

    public class ThemeSelectionViewModel
    {
        [Required]
        public string ActiveThemeMode { get; set; } = ThemeMode.System;

        [Required]
        public string ActiveThemeName { get; set; } = "Orange";
    }

    public class OllamaSettingsViewModel
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string? DefaultChatModel { get; set; }
        public bool IsOnline { get; set; }
        public string? Version { get; set; }
        public string? ErrorMessage { get; set; }
        public List<OllamaRunningModelViewModel> RunningModels { get; set; } = new();
        public List<OllamaInstalledModelViewModel> InstalledModels { get; set; } = new();
    }

    public class OllamaRunningModelViewModel
    {
        public string Name { get; set; } = string.Empty;
        public long SizeVram { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }

    public class OllamaInstalledModelViewModel
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? ParameterSize { get; set; }
        public string? QuantizationLevel { get; set; }
        public string? Family { get; set; }
        public DateTimeOffset? ModifiedAt { get; set; }
        public bool IsRunning { get; set; }
    }

    public class PullOllamaModelViewModel
    {
        [Required(ErrorMessage = "Modelnavn er påkrævet")]
        [StringLength(200)]
        public string ModelName { get; set; } = string.Empty;
    }

    public class SmsSettingsViewModel
    {
        public bool IsOnline { get; set; }
        public string? ErrorMessage { get; set; }
        public string BaseUrl { get; set; } = string.Empty;
        public decimal? BalanceDkk { get; set; }
        public DateTime? BalanceUpdatedAtUtc { get; set; }
        public decimal? CurrentPriceDkk { get; set; }
        public DateTime? CurrentPriceUpdatedAtUtc { get; set; }
        public SmsFilterViewModel Messages { get; set; } = new();
    }

    public class SmsListItemViewModel
    {
        public int Id { get; set; }
        public Guid? GatewayMessageId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int SegmentCount { get; set; }
        public decimal? TotalPriceDkk { get; set; }
        public string? FailureReason { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }

    public class SmsFilterViewModel
    {
        public string? SearchText { get; set; }
        public string? Direction { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<SmsListItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class SendSmsViewModel
    {
        [Required(ErrorMessage = "Telefonnummer er påkrævet")]
        [Phone(ErrorMessage = "Ugyldigt telefonnummer")]
        [StringLength(20, ErrorMessage = "Telefonnummer må ikke være længere end 20 tegn")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "Besked er påkrævet")]
        [StringLength(1000, ErrorMessage = "Besked må ikke være længere end 1000 tegn")]
        public string Message { get; set; } = string.Empty;
    }

    public class SmsSubscriptionViewModel
    {
        public Guid SubscriptionId { get; set; }
        public List<string> PhoneNumbers { get; set; } = new();
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
        public string? WebhookUrl { get; set; }
        public bool IsActive { get; set; }
    }

    public class SmsSubscriberNumbersViewModel
    {
        public List<SmsSubscriptionViewModel> Subscriptions { get; set; } = new();
    }

    public class SaveSmsSubscriberNumbersViewModel
    {
        public Guid? SubscriptionId { get; set; }
        public List<string> PhoneNumbers { get; set; } = new();

        [Required(ErrorMessage = "Startdato er påkrævet")]
        public DateOnly StartDate { get; set; }

        [Required(ErrorMessage = "Slutdato er påkrævet")]
        public DateOnly EndDate { get; set; }

        public string? WebhookUrl { get; set; }
    }

    public class MailSettingsViewModel
    {
        public bool IsOnline { get; set; }
        public string? ErrorMessage { get; set; }
        public int TotalMessageCount { get; set; }
        public int UnseenMessageCount { get; set; }
        public MailFilterViewModel Messages { get; set; } = new();
    }

    public class MailListItemViewModel
    {
        public uint Uid { get; set; }
        public string From { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public bool IsSeen { get; set; }
        public List<MailAttachmentListItemViewModel> Attachments { get; set; } = new();
    }

    public class MailAttachmentListItemViewModel
    {
        public string FileName { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
    }

    public class MailMessageViewModel
    {
        public uint Uid { get; set; }
        public string From { get; set; } = string.Empty;
        public List<string> To { get; set; } = new();
        public string Subject { get; set; } = string.Empty;
        public DateTime ReceivedAtUtc { get; set; }
        public string? TextBody { get; set; }
        public string? HtmlBody { get; set; }
        public List<MailAttachmentListItemViewModel> Attachments { get; set; } = new();
    }

    public class MailFilterViewModel
    {
        public string? SearchText { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<MailListItemViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class SendMailViewModel
    {
        [Required(ErrorMessage = "Modtager er påkrævet")]
        [StringLength(500, ErrorMessage = "Modtagerliste må ikke være længere end 500 tegn")]
        public string To { get; set; } = string.Empty;

        [Required(ErrorMessage = "Emne er påkrævet")]
        [StringLength(200, ErrorMessage = "Emne må ikke være længere end 200 tegn")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Besked er påkrævet")]
        [StringLength(10000, ErrorMessage = "Besked må ikke være længere end 10000 tegn")]
        public string Body { get; set; } = string.Empty;
    }

    public class AiGatewaySettingsViewModel
    {
        public string BaseUrl { get; set; } = string.Empty;
        public string? DefaultChatModel { get; set; }
        public bool IsOnline { get; set; }
        public string? Status { get; set; }
        public string? ErrorMessage { get; set; }
        public List<AiGatewayHealthCheckViewModel> HealthChecks { get; set; } = new();

        public List<AiGatewayGroupViewModel> Groups { get; set; } = new();
        public List<AiGatewayDocumentViewModel> Documents { get; set; } = new();

        public List<AiGatewayOllamaModelViewModel> OllamaModels { get; set; } = new();
        public List<AiGatewaySpeachesModelViewModel> SpeachesModels { get; set; } = new();
    }

    public class AiGatewayHealthCheckViewModel
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
        public string? Description { get; set; }
    }

    public class AiGatewayGroupViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAtUtc { get; set; }
        public int DocumentCount { get; set; }
    }

    public class AiGatewayDocumentViewModel
    {
        public Guid Id { get; set; }
        public Guid GroupId { get; set; }
        public string? GroupName { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long SizeBytes { get; set; }
        public string? Status { get; set; }
        public DateTime UploadedAtUtc { get; set; }
    }

    public class CreateAiGatewayGroupViewModel
    {
        [Required(ErrorMessage = "Navn er påkrævet")]
        [StringLength(200, MinimumLength = 1)]
        public string Name { get; set; } = string.Empty;
    }

    public class UploadAiGatewayDocumentViewModel
    {
        [Required]
        public Guid GroupId { get; set; }

        [Required(ErrorMessage = "Vælg en fil")]
        public IFormFile? File { get; set; }
    }

    public class AiGatewayOllamaModelViewModel
    {
        public string Name { get; set; } = string.Empty;
        public long Size { get; set; }
        public string? ParameterSize { get; set; }
        public string? QuantizationLevel { get; set; }
        public string? Family { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public bool IsRunning { get; set; }
    }

    public class AiGatewaySpeachesModelViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string? OwnedBy { get; set; }
        public string? Task { get; set; }
        public List<string> Language { get; set; } = new();
        public bool IsRunning { get; set; }
    }

    public class LogFilterViewModel
    {
        public string? Level { get; set; }
        public string? SearchText { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public List<LogEntryViewModel> Entries { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class LogEntryViewModel
    {
        public long Id { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string Level { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        public string? Properties { get; set; }
    }
}
