namespace AuthenticationOpenIddict.API.Options
{
    public class AppInfoOptions
    {
        public const string Section = nameof(AppInfoOptions);
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
