namespace Volo.Authentication.OpenIddict.Models
{
    public class InviteUserResponseModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public int Role { get; set; }
        public string Type { get; set; }
    }
}
