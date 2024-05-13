namespace AuthenticationProject.API.Models
{
    public class VerifyTokenModel
    {
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
        public string Type { get; set; } = default!;
    }
}
