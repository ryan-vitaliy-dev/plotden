using backend.Application.Common;
using backend.Infrastructure.Common;
using backend.Infrastructure.Email;
using backend.Resources;
using Microsoft.Extensions.Localization;

namespace backend.Application.Email
{

    public class EmailService(IEmailSender emailSender, ILogger<EmailService> logger, IStringLocalizer<SharedResource> localizer, IConfiguration configuration)
    {
        private readonly TimeSpan _emailVerificationTokenDuration = TimeSpan.Parse(configuration["Tokens:EmailVerification:ExpiresIn"] ?? throw new InvalidOperationException("Tokens:EmailVerification:ExpiresIn is not configured."));

        private readonly TimeSpan _resumeSignupTokenDuration = TimeSpan.Parse(configuration["Tokens:ResumeSignup:ExpiresIn"] ?? throw new InvalidOperationException("Tokens:ResumeSignup:ExpiresIn is not configured."));

        private readonly IEmailSender _emailSender = emailSender;

        private readonly ILogger<EmailService> _logger = logger;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        

        public async Task<ServiceResult<Unit>> SendVerificationEmailAsync(string emailAddress, string rawToken, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectVerifyEmailAddress"].Value;
            string emailBody = EmailTemplateLoader.LoadTemplate("SignupVerificationLink.html", new Dictionary<string, string>
            {
                ["VerificationLink"] = "plotden.com/verify?token=" + rawToken,
                ["TimeValue"] = _emailVerificationTokenDuration.Minutes.ToString(),
                ["TimeUnit"] = "minutes"
            });
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        public async Task<ServiceResult<Unit>> SendAccountExistsEmailAsync(string emailAddress, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectAccountAlreadyExists"].Value;
            string emailBody = EmailTemplateLoader.LoadTemplate("SignupAttemptAccountExists.html");
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        public async Task<ServiceResult<Unit>> SendAccountExistsResumeSignupEmailAsync(string emailAddress, string rawToken, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectAccountAlreadyExists"].Value;
            string emailBody = EmailTemplateLoader.LoadTemplate("SignupAttemptAccountIncomplete.html", new Dictionary<string, string>
            {
                ["SignupResumeLink"] = "plotden.com/resume?token=" + rawToken,
                ["TimeValue"] = _resumeSignupTokenDuration.Minutes.ToString(),
                ["TimeUnit"] = "minutes"
            });
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        private async Task<ServiceResult<Unit>> SendEmailAsync(EmailMessage emailMessage, CancellationToken clt)
        {
            bool emailSent = await _emailSender.SendAsync(emailMessage, clt);
            if(emailSent)
            {
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            else
            {
                _logger.LogWarning("\"{EmailSubject}\" email failed to be sent to email {Email}.", emailMessage.Subject, emailMessage.To);
                return ServiceResult<Unit>.Failure(ServiceError.UnknownError);
            }
        }
    }
}