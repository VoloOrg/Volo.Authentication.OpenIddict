namespace Volo.Authentication.OpenIddict.Options
{
    public class MailingOptions
    {
        public const string Section = nameof(MailingOptions);
        public string EnvironmentUri { get; set; }
        public string ResetEndpoint { get; set; }
        public string RegisterEndpoint { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}
