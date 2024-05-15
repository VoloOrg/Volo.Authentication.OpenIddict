namespace AuthenticationOpenIddict.API.Mailing
{
    public class SendEmailModel
    {
        public string FromEmail { get; set; } = default!;
        public string FromName { get; set; } = default!;
        public string ToEmail { get; set; } = default!;
        public string ToName { get; set; } = default!;
        public string Subject { get; set; } = default!;
        public string PlainTextMessage { get; set; } = default!;
        public string HtmlTextMessage { get; set; } = default!;
    }
}
