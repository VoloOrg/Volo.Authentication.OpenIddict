using System.ComponentModel.DataAnnotations;

namespace AuthenticationOpenIddict.API.Models
{
    public class InviteUserModel
    {
        [EmailAddress]
        public string Email { get; set; } = default!;
        public int Role { get; set; }
    }
}
