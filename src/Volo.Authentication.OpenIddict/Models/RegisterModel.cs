using System.ComponentModel.DataAnnotations;

namespace AuthenticationOpenIddict.Models
{
    public class RegisterModel
    {
        [EmailAddress]
        public string Email { get; set; }
        public string Password { get; set; }
        public int Role { get; set; }
    }
}
