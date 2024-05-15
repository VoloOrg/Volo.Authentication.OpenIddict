namespace AuthenticationOpenIddict.API.Mailing
{
    public interface IMailingService
    {
        Task<EmailResponseModel> SendEmailAsync(SendEmailModel data, CancellationToken cancellationToken);
    }
}
