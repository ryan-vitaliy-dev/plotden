using Application.Common;
using Application.Common.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Infrastructure.Email
{
    public class FluentEmailSender(ILogger<FluentEmailSender> logger, IConfiguration configuration) : IEmailSender
    {
        private readonly ILogger<FluentEmailSender> _logger = logger;

        private readonly string _senderName = configuration["Email:SenderName"] ?? throw new InvalidOperationException("Email:SenderName is not configured.");
        private readonly string _senderAddress = configuration["Email:SenderAddress"] ?? throw new InvalidOperationException("Email:SenderAddress is not configured.");
        private readonly string _smtpServerHost = configuration["Email:SmtpHost"] ?? throw new InvalidOperationException("Email:SmtpHost is not configured.");
        private readonly int _port = int.Parse(configuration["Email:SmtpPort"] ?? throw new InvalidOperationException("Email:SmtpPort is not configured."));

        public async Task<bool> SendAsync(EmailMessage email, CancellationToken clt)
        {
            MimeMessage message = new();
            try
            {
                message.From.Add(new MailboxAddress(_senderName, _senderAddress));
                message.To.Add(MailboxAddress.Parse(email.To));
                message.Subject = email.Subject;
                BodyBuilder bodyBuilder = new()
                {
                    HtmlBody = email.HtmlBody
                };
                if (email.PlainTextBody != null)
                {
                    bodyBuilder.TextBody = email.PlainTextBody;
                }
                message.Body = bodyBuilder.ToMessageBody();
                using SmtpClient client = new();
                using CancellationTokenSource timeoutClts = new(TimeSpan.FromSeconds(5));
                using CancellationTokenSource linkedClt = CancellationTokenSource.CreateLinkedTokenSource(clt, timeoutClts.Token);
                CancellationToken linkedToken = linkedClt.Token;
                await client.ConnectAsync(_smtpServerHost, _port, MailKit.Security.SecureSocketOptions.None, linkedToken);
                await client.SendAsync(message, linkedToken);
                await client.DisconnectAsync(true, linkedToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception when sending email.");
                return false;
            }
        }
    }
}