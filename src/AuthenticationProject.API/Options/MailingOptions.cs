namespace AuthenticationProject.API.Options
{
    public class MailingOptions
    {
        public const string Section = nameof(MailingOptions);
        public string EnvironmentUri { get; set; }
        public string Endpoint { get; set; }
        public string FromEmail { get; set; }
        public string FromName { get; set; }
    }
}
