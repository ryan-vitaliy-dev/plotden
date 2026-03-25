namespace backend.Infrastructure.Email
{
    public interface IEmailSender
    {
        Task<bool> SendAsync(EmailMessage email, CancellationToken? clt);
    }
}