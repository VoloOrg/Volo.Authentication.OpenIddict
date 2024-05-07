namespace AuthenticationProject.API.Options
{
    public class AppInfo
    {
        public const string Section = nameof(AppInfo);
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; }
    }
}
