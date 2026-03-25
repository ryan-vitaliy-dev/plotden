using MailKit.Net.Smtp;
using MimeKit;

namespace backend.Infrastructure.Email
{
    public class FluentEmailSender : IEmailSender
    {
        private readonly string _smtpServerHost = "192.168.4.93";

        private readonly int _port = 25;
        
        public async Task<bool> SendAsync(EmailMessage email, CancellationToken? clt)
        {
            // throw new NotImplementedException();
            var message = new MimeMessage();
            try
            {
                message.From.Add(new MailboxAddress("Plotden", "no-reply@plotden.com"));
                message.To.Add(MailboxAddress.Parse(email.To));
                message.Subject = email.Subject;
                message.Body = new TextPart("plain")
                {
                    Text = email.Body
                };
                using var client = new SmtpClient();
                var timeoutCancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                var linkedCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(clt ?? CancellationToken.None, timeoutCancellationTokenSource.Token).Token;
                await client.ConnectAsync(_smtpServerHost, _port, MailKit.Security.SecureSocketOptions.None, linkedCancellationToken);
                await client.SendAsync(message, linkedCancellationToken);
                await client.DisconnectAsync(true, linkedCancellationToken);
                return true;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception when sending email:" + ex);
                return false;
            }
        }
    }
}