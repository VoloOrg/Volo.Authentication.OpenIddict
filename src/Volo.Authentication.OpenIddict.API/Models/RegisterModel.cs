namespace Volo.Authentication.OpenIddict.API.Models
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
    }
}
