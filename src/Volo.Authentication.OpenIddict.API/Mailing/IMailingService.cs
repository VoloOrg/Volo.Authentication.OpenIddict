namespace Volo.Authentication.OpenIddict.API.Mailing
{
    public interface IMailingService
    {
        Task<EmailResponseModel> SendEmailAsync(SendEmailModel data, CancellationToken cancellationToken);
    }
}
