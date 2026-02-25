using MailKit.Net.Smtp;
using MimeKit;

namespace backend.Infrastructure.Email
{
    public class FluentEmailSender : IEmailSender
    {
        private readonly string _smtpServerHost = "192.168.4.93";

        private readonly int _port = 25;
        
        public async Task<bool> SendAsync(EmailMessage email)
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
                var client = new SmtpClient();
                var clt = new CancellationToken();
                await client.ConnectAsync(_smtpServerHost, _port, MailKit.Security.SecureSocketOptions.None, clt);
                await client.SendAsync(message, clt);
                await client.DisconnectAsync(true, clt);
                return true;
            }
            catch (OperationCanceledException)
            {
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