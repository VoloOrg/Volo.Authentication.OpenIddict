using System.ComponentModel.DataAnnotations;

namespace AuthenticationProject.API.Models
{
    public class InviteUserModel
    {
        [EmailAddress]
        public string Email { get; set; } = default!;
        public int Role { get; set; }
    }
}
