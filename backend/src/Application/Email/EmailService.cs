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

        private readonly TimeSpan _passwordResetTokenDuration = TimeSpan.Parse(
            configuration["Tokens:PasswordReset:ExpiresIn"] ?? throw new InvalidOperationException("Tokens:PasswordReset:ExpiresIn is not configured.")
        );

        private readonly IEmailSender _emailSender = emailSender;
        private readonly IEmailTemplateLoader _emailTemplateLoader = emailTemplateLoader;

        private readonly ILogger<EmailService> _logger = logger;
        private readonly IStringLocalizer<SharedResource> _localizer = localizer;


        // Overload for omitting token
        public Task<ServiceResult<Unit>> SendEmailAsync(string emailAddress, EmailTemplate emailTemplate, CancellationToken clt)
            => SendEmailAsync(emailAddress, emailTemplate, null!, clt);

        public async Task<ServiceResult<Unit>> SendEmailAsync(string emailAddress, EmailTemplate emailTemplate, string rawToken, CancellationToken clt)
        {
            var (emailSubject, emailTemplateFile, emailVariables) = emailTemplate switch
            {
                EmailTemplate.EmailVerification => (
                    _localizer["Email_Subject_EmailVerification"].Value, 
                    "EmailVerification.html",
                    new Dictionary<string, string>
                    {
                        ["VerificationLink"] = "plotden.com/signup/verify?token=" + rawToken,
                        ["TimeValue"] = _emailVerificationTokenDuration.Minutes.ToString(),
                        ["TimeUnit"] = "minutes"
                    }
                ),
                EmailTemplate.IncompleteAccountSignup => (
                    _localizer["Email_Subject_IncompleteAccountSignup"].Value, 
                    "IncompleteAccountSignup.html",
                    new Dictionary<string, string>
                    {
                        ["ResumeLink"] = "plotden.com/signup/resume?token=" + rawToken,
                        ["TimeValue"] = _resumeSignupTokenDuration.Minutes.ToString(),
                        ["TimeUnit"] = "minutes"
                    }
                ),
                EmailTemplate.ExistingAccountSignup => (
                    _localizer["Email_Subject_ExistingAccountSignup"].Value, 
                    "ExistingAccountSignup.html",
                    []
                ),
                EmailTemplate.IncompleteAccountRecovery => (
                    _localizer["Email_Subject_IncompleteAccountRecovery"].Value, 
                    "IncompleteAccountRecovery.html",
                    new Dictionary<string, string>
                    {
                        ["RecoveryLink"] = "plotden.com/account/recover?token=" + rawToken,
                        ["TimeValue"] = _resumeSignupTokenDuration.Minutes.ToString(),
                        ["TimeUnit"] = "minutes"
                    }
                ),
                EmailTemplate.EmailUpdate => throw new NotImplementedException("Not implemented yet."),
                EmailTemplate.PasswordReset => (
                    _localizer["Email_Subject_PasswordReset"].Value, 
                    "PasswordReset.html",
                    new Dictionary<string, string>
                    {
                        ["ResetLink"] = "plotden.com/account/reset-password?token=" + rawToken,
                        ["TimeValue"] = _passwordResetTokenDuration.Minutes.ToString(),
                        ["TimeUnit"] = "minutes"
                    }
                ),
                _ => throw new ArgumentOutOfRangeException(nameof(emailTemplate))
            };

            
            string emailBody = _emailTemplateLoader.LoadTemplate(emailTemplateFile, emailVariables);
            EmailMessage emailMessage = new(emailAddress, emailSubject, emailBody);

            bool emailSent = await _emailSender.SendAsync(emailMessage, clt);
            if(emailSent)
            {
                return ServiceResult<Unit>.Success(Unit.Value);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send {EmailTemplate} email to address {Email}", 
                    emailTemplate, 
                    emailMessage.To
                );
                return ServiceResult<Unit>.Failure(ServiceError.UnknownError);
            }
        }
    }
}