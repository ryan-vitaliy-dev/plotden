using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Configuration;

using Application.Common;
using Application.Common.Interfaces;
using Application.Resources;
using Domain.Common;

namespace Application.Email
{

    public class EmailService(
        IEmailSender emailSender, 
        IEmailTemplateLoader emailTemplateLoader, 
        ILogger<EmailService> logger, 
        IStringLocalizer<SharedResource> localizer, 
        IConfiguration configuration)
    {
        private readonly TimeSpan _emailVerificationTokenDuration = TimeSpan.Parse(
            configuration["Tokens:EmailVerification:ExpiresIn"] ?? throw new InvalidOperationException("Tokens:EmailVerification:ExpiresIn is not configured.")
        );

        private readonly TimeSpan _resumeSignupTokenDuration = TimeSpan.Parse(
            configuration["Tokens:ResumeSignup:ExpiresIn"] ?? throw new InvalidOperationException("Tokens:ResumeSignup:ExpiresIn is not configured.")
        );

        private readonly IEmailSender _emailSender = emailSender;
        private readonly IEmailTemplateLoader _emailTemplateLoader = emailTemplateLoader;

        private readonly ILogger<EmailService> _logger = logger;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;
        

        public async Task<ServiceResult<Unit>> SendVerificationEmailAsync(string emailAddress, string rawToken, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectVerifyEmailAddress"].Value;
            string emailBody = _emailTemplateLoader.LoadTemplate("SignupVerificationLink.html", new Dictionary<string, string>
            {
                ["VerificationLink"] = "plotden.com/signup/verify?token=" + rawToken,
                ["TimeValue"] = _emailVerificationTokenDuration.Minutes.ToString(),
                ["TimeUnit"] = "minutes"
            });
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        public async Task<ServiceResult<Unit>> SendAccountExistsEmailAsync(string emailAddress, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectAccountAlreadyExists"].Value;
            string emailBody = _emailTemplateLoader.LoadTemplate("SignupAttemptAccountExists.html");
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        // TODO: This is currently used for the case where some other user requests while the first user hasnt yet set a password.
        // However, it's also used when the user themselves hasnt set a password and tries to recover their account. We should probably split into two separate methods and email templates for better clarity and user experience.
        public async Task<ServiceResult<Unit>> SendAccountExistsResumeSignupEmailAsync(string emailAddress, string rawToken, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectAccountAlreadyExists"].Value;
            string emailBody = _emailTemplateLoader.LoadTemplate("SignupAttemptAccountIncomplete.html", new Dictionary<string, string>
            {
                ["SignupResumeLink"] = "plotden.com/signup/resume?token=" + rawToken,
                ["TimeValue"] = _resumeSignupTokenDuration.Minutes.ToString(),
                ["TimeUnit"] = "minutes"
            });
            return await SendEmailAsync(new EmailMessage(emailAddress, emailSubject, emailBody), clt);
        }

        public async Task<ServiceResult<Unit>> SendAccountExistsResumeSignupRecoveryEmailAsync(string emailAddress, string rawToken, CancellationToken clt)
        {
            string emailSubject = _localizer["Email_SubjectAccountRecovery"].Value;
            string emailBody = _emailTemplateLoader.LoadTemplate("RecoverResumeSignup.html", new Dictionary<string, string>
            {
                ["SignupResumeLink"] = "plotden.com/signup/resume?token=" + rawToken,
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