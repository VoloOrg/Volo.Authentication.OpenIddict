namespace AuthenticationProject.Models
{
    public class VerifyForgotPasswordTokenModel
    {
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}
