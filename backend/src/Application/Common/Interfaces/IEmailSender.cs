namespace Application.Common.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(EmailMessage email, CancellationToken clt);
    }
}