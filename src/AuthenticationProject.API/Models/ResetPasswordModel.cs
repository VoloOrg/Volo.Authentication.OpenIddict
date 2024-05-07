namespace AuthenticationProject.API.Models
{
    public class ResetPasswordModel
    {
        public string Password { get; set; } = default!;
        public string ConfirmPassword { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}
