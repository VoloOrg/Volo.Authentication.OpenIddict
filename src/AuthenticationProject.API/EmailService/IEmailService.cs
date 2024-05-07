namespace AuthenticationProject.API.EmailService
{
    public interface IEmailService
    {
        Task<string> SendEmailAsync();
    }
}
