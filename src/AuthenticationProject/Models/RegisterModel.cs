using System.ComponentModel.DataAnnotations;

namespace AuthenticationProject.Models
{
    public class RegisterModel
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
    }
}
