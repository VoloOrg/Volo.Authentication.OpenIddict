namespace Volo.Authentication.OpenIddict.Mailing
{
    public interface IMailingService
    {
        Task<EmailResponseModel> SendEmailAsync(SendEmailModel data, CancellationToken cancellationToken);
    }
}
