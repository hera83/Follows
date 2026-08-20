using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using MimeKit.Utils;
using web.Services.Mail.Dtos;
using IMailService = web.Services.Mail.Interfaces.IMailService;

namespace web.Services.Mail;

public class MailService : IMailService
{
    private readonly IConfiguration _configuration;

    public MailService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<SendMailResponseDto> SendMailAsync(SendMailRequestDto request, CancellationToken cancellationToken = default)
    {
        var fromAddress = _configuration["Mail:Smtp:FromAddress"]
            ?? throw new InvalidOperationException("Mail:Smtp:FromAddress is not configured.");
        var fromName = _configuration["Mail:Smtp:FromName"];

        var message = new MimeMessage
        {
            MessageId = MimeUtils.GenerateMessageId(),
            Date = DateTimeOffset.UtcNow,
            Subject = request.Subject,
        };
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.AddRange(request.To.Select(MailboxAddress.Parse));
        message.Cc.AddRange(request.Cc.Select(MailboxAddress.Parse));
        message.Bcc.AddRange(request.Bcc.Select(MailboxAddress.Parse));
        if (!string.IsNullOrWhiteSpace(request.ReplyTo))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(request.ReplyTo));
        }

        var bodyBuilder = new BodyBuilder
        {
            TextBody = request.TextBody,
            HtmlBody = request.HtmlBody,
        };
        foreach (var attachment in request.Attachments)
        {
            bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
        }
        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();
        await ConnectSmtpAsync(client, cancellationToken);
        try
        {
            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }

        return new SendMailResponseDto
        {
            MessageId = message.MessageId,
            SentAtUtc = DateTime.UtcNow,
        };
    }

    public async Task<IReadOnlyList<ReadMailResponseDto>> ReadMailAsync(ReadMailRequestDto? request = null, CancellationToken cancellationToken = default)
    {
        request ??= new ReadMailRequestDto();

        using var client = new ImapClient();
        await ConnectImapAsync(client, cancellationToken);

        try
        {
            var folder = string.Equals(request.Folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(request.Folder, cancellationToken);

            await folder.OpenAsync(request.MarkAsSeen ? FolderAccess.ReadWrite : FolderAccess.ReadOnly, cancellationToken);

            var query = request.UnseenOnly ? SearchQuery.NotSeen : SearchQuery.All;
            var uids = await folder.SearchAsync(query, cancellationToken);
            if (request.MaxCount > 0 && uids.Count > request.MaxCount)
            {
                uids = uids.Skip(uids.Count - request.MaxCount).ToList();
            }

            if (uids.Count == 0)
            {
                return [];
            }

            var summaries = await folder.FetchAsync(uids, MessageSummaryItems.UniqueId | MessageSummaryItems.Flags, cancellationToken);
            var flagsByUid = summaries.ToDictionary(s => s.UniqueId, s => s.Flags ?? MessageFlags.None);

            var results = new List<ReadMailResponseDto>();
            foreach (var uid in uids)
            {
                var message = await folder.GetMessageAsync(uid, cancellationToken);
                var isSeen = flagsByUid.TryGetValue(uid, out var flags) && flags.HasFlag(MessageFlags.Seen);

                results.Add(MapMessage(uid, message, isSeen));

                if (request.MarkAsSeen && !isSeen)
                {
                    await folder.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                }
            }

            return results;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    public async Task<ReadMailResponseDto?> GetMailAsync(uint uid, string folder = "INBOX", bool markAsSeen = false, CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await ConnectImapAsync(client, cancellationToken);

        try
        {
            var mailFolder = string.Equals(folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folder, cancellationToken);

            await mailFolder.OpenAsync(markAsSeen ? FolderAccess.ReadWrite : FolderAccess.ReadOnly, cancellationToken);

            var uniqueId = new UniqueId(uid);
            try
            {
                var message = await mailFolder.GetMessageAsync(uniqueId, cancellationToken);
                var summary = await mailFolder.FetchAsync([uniqueId], MessageSummaryItems.Flags, cancellationToken);
                var isSeen = summary.Count > 0 && (summary[0].Flags ?? MessageFlags.None).HasFlag(MessageFlags.Seen);

                if (markAsSeen && !isSeen)
                {
                    await mailFolder.AddFlagsAsync(uniqueId, MessageFlags.Seen, true, cancellationToken);
                    isSeen = true;
                }

                return MapMessage(uniqueId, message, isSeen);
            }
            catch (MessageNotFoundException)
            {
                return null;
            }
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    public async Task<MailFolderStatusDto> GetFolderStatusAsync(string folder = "INBOX", CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await ConnectImapAsync(client, cancellationToken);

        try
        {
            var mailFolder = string.Equals(folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folder, cancellationToken);

            await mailFolder.OpenAsync(FolderAccess.ReadOnly, cancellationToken);
            await mailFolder.StatusAsync(StatusItems.Count | StatusItems.Unread, cancellationToken);

            return new MailFolderStatusDto
            {
                TotalCount = mailFolder.Count,
                UnseenCount = mailFolder.Unread,
            };
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    private static ReadMailResponseDto MapMessage(UniqueId uid, MimeMessage message, bool isSeen)
    {
        var attachments = new List<MailAttachmentDto>();
        foreach (var part in message.Attachments.OfType<MimePart>())
        {
            if (part.Content is null)
            {
                continue;
            }

            using var memoryStream = new MemoryStream();
            part.Content.DecodeTo(memoryStream);
            attachments.Add(new MailAttachmentDto
            {
                FileName = part.FileName ?? "attachment",
                ContentType = part.ContentType.MimeType,
                Content = memoryStream.ToArray(),
            });
        }

        return new ReadMailResponseDto
        {
            Uid = uid.Id,
            MessageId = message.MessageId,
            From = message.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
            To = message.To.Mailboxes.Select(m => m.Address).ToList(),
            Subject = message.Subject ?? string.Empty,
            TextBody = message.TextBody,
            HtmlBody = message.HtmlBody,
            ReceivedAtUtc = message.Date.UtcDateTime,
            IsSeen = isSeen,
            Attachments = attachments,
        };
    }

    public async Task<bool> DeleteMailAsync(uint uid, string folder = "INBOX", CancellationToken cancellationToken = default)
    {
        using var client = new ImapClient();
        await ConnectImapAsync(client, cancellationToken);

        try
        {
            var mailFolder = string.Equals(folder, "INBOX", StringComparison.OrdinalIgnoreCase)
                ? client.Inbox
                : await client.GetFolderAsync(folder, cancellationToken);

            await mailFolder.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uniqueId = new UniqueId(uid);
            try
            {
                await mailFolder.AddFlagsAsync(uniqueId, MessageFlags.Deleted, true, cancellationToken);
            }
            catch (MessageNotFoundException)
            {
                return false;
            }

            await mailFolder.ExpungeAsync([uniqueId], cancellationToken);
            return true;
        }
        finally
        {
            await client.DisconnectAsync(true, cancellationToken);
        }
    }

    private async Task ConnectSmtpAsync(SmtpClient client, CancellationToken cancellationToken)
    {
        var host = _configuration["Mail:Smtp:Host"]
            ?? throw new InvalidOperationException("Mail:Smtp:Host is not configured.");
        var port = _configuration.GetValue<int?>("Mail:Smtp:Port") ?? 587;
        var username = _configuration["Mail:Smtp:Username"];
        var password = _configuration["Mail:Smtp:Password"];
        var secureSocketOptions = ParseSecureSocketOptions(_configuration["Mail:Smtp:SecureSocketOptions"]);

        await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password ?? string.Empty, cancellationToken);
        }
    }

    private async Task ConnectImapAsync(ImapClient client, CancellationToken cancellationToken)
    {
        var host = _configuration["Mail:Imap:Host"]
            ?? throw new InvalidOperationException("Mail:Imap:Host is not configured.");
        var port = _configuration.GetValue<int?>("Mail:Imap:Port") ?? 993;
        var username = _configuration["Mail:Imap:Username"];
        var password = _configuration["Mail:Imap:Password"];
        var secureSocketOptions = ParseSecureSocketOptions(_configuration["Mail:Imap:SecureSocketOptions"]);

        await client.ConnectAsync(host, port, secureSocketOptions, cancellationToken);
        if (!string.IsNullOrWhiteSpace(username))
        {
            await client.AuthenticateAsync(username, password ?? string.Empty, cancellationToken);
        }
    }

    private static SecureSocketOptions ParseSecureSocketOptions(string? value)
    {
        return Enum.TryParse<SecureSocketOptions>(value, ignoreCase: true, out var result)
            ? result
            : SecureSocketOptions.Auto;
    }
}
