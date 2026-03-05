using SparkService.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using MongoDB.Bson;

namespace SparkService.Services
{
    public class MailService
    {
        private readonly ILogger<MailService> _logger;
        private readonly MailSettings _settings;
        private readonly EmailTemplateService _emailTemplateService;

        public MailService(ILogger<MailService> logger, IOptions<MailSettings> settings, EmailTemplateService emailTemplateService) =>
        (_logger, _settings, _emailTemplateService) = (logger, settings.Value, emailTemplateService);


        public async Task<bool> SendAsync(MailData mailData, CancellationToken ct = default)
        {
            try
            {
                // Initialize a new instance of the MimeKit.MimeMessage class
                var mail = new MimeMessage();

                #region Sender / Receiver
                // Sender
                mail.From.Add(new MailboxAddress(_settings.DisplayName, mailData.From ?? _settings.From));
                mail.Sender = new MailboxAddress(mailData.DisplayName ?? _settings.DisplayName, mailData.From ?? _settings.From);

                // Receiver
                foreach (string mailAddress in mailData.To)
                    mail.To.Add(MailboxAddress.Parse(mailAddress));

                // Set Reply to if specified in mail data
                if (!string.IsNullOrEmpty(mailData.ReplyTo))
                    mail.ReplyTo.Add(new MailboxAddress(mailData.ReplyToName, mailData.ReplyTo));

                // BCC
                // Check if a BCC was supplied in the request
                if (mailData.Bcc != null)
                {
                    // Get only addresses where value is not null or with whitespace. x = value of address
                    foreach (string mailAddress in mailData.Bcc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Bcc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }

                // CC
                // Check if a CC address was supplied in the request
                if (mailData.Cc != null)
                {
                    foreach (string mailAddress in mailData.Cc.Where(x => !string.IsNullOrWhiteSpace(x)))
                        mail.Cc.Add(MailboxAddress.Parse(mailAddress.Trim()));
                }
                #endregion

                #region Content

                // Add Content to Mime Message
                var body = new BodyBuilder();
                mail.Subject = mailData.Subject;
                body.HtmlBody = mailData.Body;
                mail.Body = body.ToMessageBody();

                #endregion

                #region Send Mail

                using var smtp = new SmtpClient();

                if (_settings.UseSSL)
                {
                    await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.SslOnConnect, ct);
                }
                else if (_settings.UseStartTls)
                {
                    await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls, ct);
                }
                await smtp.AuthenticateAsync(_settings.UserName, _settings.Password, ct);
                await smtp.SendAsync(mail, ct);
                await smtp.DisconnectAsync(true, ct);

                #endregion

                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendAsync Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailVerification(User user, Profile profile, EmailVerificationRequests emailVerificationRequests)
        {
            try
            {

                var template = await _emailTemplateService.GetAsync("EMAIL_VERIFICATION");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[VERIFICATION_LINK]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[VERIFICATION_LINK]$$</a></span>", string.Format("<a style=\"color: white;\" href='https://app.happysugardaddy.com/api/v1/account/email-verification?code={0}'>Confirm Email</a>", emailVerificationRequests.token));
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendEmailVerification Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSubscriptionPaymentSuccess(User user, Profile profile, SubscriptionPayments subscriptionPayments, Subscriptions subscriptions, SubscriptionPlans subscriptionPlans, string transactionId)
        {
            try
            {

                var template = await _emailTemplateService.GetAsync("SUBSCRIPTION_PAYMENT_SUCCESS");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[TRANSACTION_ID]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[TRANSACTION_ID]$$</a></span>", transactionId);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[TOTAL]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[TOTAL]$$</a></span>", subscriptionPayments.amount.ToString("0.00"));
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_NAME]$$</a></span>", subscriptionPlans.name);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[SUB_TOTAL]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[SUB_TOTAL]$$</a></span>", subscriptionPayments.amount.ToString("0.00"));


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendSubscriptionPaymentSuccess Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSubscriptionRenewalSuccess(User user, Profile profile, SubscriptionPlans subscriptionPlans)
        {
            try
            {
                var template = await _emailTemplateService.GetAsync("SUBSCRIPTION_RENEWAL");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[TOTAL]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[TOTAL]$$</a></span>", subscriptionPlans.price.ToString("0.00"));
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_NAME]$$</a></span>", subscriptionPlans.name);


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendSubscriptionRenewalSuccess Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSubscriptionCreatedSuccess(User user, Profile profile, SubscriptionPlans subscriptionPlans, List<SubscriptionServices> subscriptionPlanServices)
        {
            try
            {
                var template = await _emailTemplateService.GetAsync("SUBSCRIPTION_CREATE");
                if (template is not null)
                {

                    string subject = template.subject;

                    var services = "<ul>";

                    foreach (SubscriptionServices serv in subscriptionPlanServices)
                    {
                        services = services + string.Format("<li> {0} </li>", serv.name);
                    }

                    services = "</ul>";

                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[TOTAL]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[TOTAL]$$</a></span>", subscriptionPlans.price.ToString("0.00"));
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_NAME]$$</a></span>", subscriptionPlans.name);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_SERVICES]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_SERVICES]$$</a></span>", services);

                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendSubscriptionRenewalSuccess Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSubscriptionCanceledSuccess(User user, Profile profile, SubscriptionPlans subscriptionPlans)
        {
            try
            {
                var template = await _emailTemplateService.GetAsync("SUBSCRIPTION_CANCEL");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_NAME]$$</a></span>", subscriptionPlans.name);


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendSubscriptionRenewalSuccess Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendSubscriptionSuspendededSuccess(User user, Profile profile, SubscriptionPlans subscriptionPlans)
        {
            try
            {
                var template = await _emailTemplateService.GetAsync("SUBSCRIPTION_SUSPENDED");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[YEAR]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[YEAR]$$</a></span>", DateTime.Now.Year.ToString());
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[PLAN_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[PLAN_NAME]$$</a></span>", subscriptionPlans.name);


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendSubscriptionRenewalSuccess Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailNewAccountFromSocialMedia(User user)
        {
            try
            {

                var template = await _emailTemplateService.GetAsync("CHANGE_PASSWORD_FOR_NEW_ACCOUNT_SOCIAL_MEDIA");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[RESET_PASSWORD_LINK]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[RESET_PASSWORD_LINK]$$</a></span>", string.Format("<a style=\"color: white;\" href='https://app.happysugardaddy.com/api/v1/account/reset-password'>Change Password</a>"));


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendEmailNewAccountFromSocialMedia Error={ex.Message}");
                return false;
            }
        }

        public async Task<bool> SendEmailForgotPassWordReset(User user, ForgotPasswordRequests forgotPasswordRequests)
        {
            try
            {

                var template = await _emailTemplateService.GetAsync("FORGOT_PASSWORD_RESET");
                if (template is not null)
                {

                    string subject = template.subject;


                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[FIRST_NAME]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[FIRST_NAME]$$</a></span>", user.username);
                    template.template = template.template.Replace("<span class=\"h-card\" data-contact=\"{&quot;name&quot;:&quot;$$[RESET_PASSWORD_LINK]$$&quot;}\"><a class=\"p-name\" href=\"javascript:void(0)\">$$[RESET_PASSWORD_LINK]$$</a></span>", string.Format("<a style=\"color: white;\" href='{0}?token={1}'>Reset Password</a>", forgotPasswordRequests.callbackUrl, forgotPasswordRequests.token));


                    string body = template.template;
                    MailData mailData = new MailData(
                         new List<string>() { user.email_address },
                         subject,
                         body,
                         _settings.From,
                        "happysugardaddy",
                         null,
                         null,
                         null,
                         null
                      );

                    return await SendAsync(mailData);
                }
                else
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"SparkService.Services.MailService.SendEmailNewAccountFromSocialMedia Error={ex.Message}");
                return false;
            }
        }

    }
}
