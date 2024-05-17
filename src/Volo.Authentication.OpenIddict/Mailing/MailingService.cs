using Microsoft.Extensions.Options;
using SendGrid.Helpers.Mail;
using SendGrid;
using Volo.Authentication.OpenIddict.Options;

namespace Volo.Authentication.OpenIddict.Mailing
{
    public class MailingService : IMailingService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly MailingOptions _mailingOptions;

        public MailingService(ISendGridClient sendGridClient, IOptions<MailingOptions> mailingOptions)
        {
            _sendGridClient = sendGridClient;
            _mailingOptions = mailingOptions.Value;
        }

        public async Task<EmailResponseModel> SendEmailAsync(SendEmailModel data, CancellationToken cancellationToken)
        {
            var from = new EmailAddress(data.FromEmail, data.FromName);
            var to = new EmailAddress(data.ToEmail, data.ToName);
            var message = MailHelper.CreateSingleEmail(from, to, data.Subject, data.PlainTextMessage, data.HtmlTextMessage);

            var response = await _sendGridClient.SendEmailAsync(message, cancellationToken);

            var res = new EmailResponseModel() { StatusCode = response?.StatusCode ?? System.Net.HttpStatusCode.InternalServerError };
            if (response?.IsSuccessStatusCode == true)
            {
                res.IsSuccess = true;
                res.StatusCode = response.StatusCode;
                res.Message = await response.Body.ReadAsStringAsync(cancellationToken);
            }
            return res;
        }
    }
}
