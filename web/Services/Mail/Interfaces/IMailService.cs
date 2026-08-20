using web.Services.Mail.Dtos;

namespace web.Services.Mail.Interfaces;

public interface IMailService
{
    Task<SendMailResponseDto> SendMailAsync(SendMailRequestDto request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReadMailResponseDto>> ReadMailAsync(ReadMailRequestDto? request = null, CancellationToken cancellationToken = default);

    Task<ReadMailResponseDto?> GetMailAsync(uint uid, string folder = "INBOX", bool markAsSeen = false, CancellationToken cancellationToken = default);

    Task<MailFolderStatusDto> GetFolderStatusAsync(string folder = "INBOX", CancellationToken cancellationToken = default);

    Task<bool> DeleteMailAsync(uint uid, string folder = "INBOX", CancellationToken cancellationToken = default);
}
