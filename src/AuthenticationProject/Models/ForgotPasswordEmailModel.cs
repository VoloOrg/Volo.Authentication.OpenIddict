using System.ComponentModel.DataAnnotations;

namespace AuthenticationProject.Models
{
    public class ForgotPasswordEmailModel
    {
        [EmailAddress]
        public string Email { get; set; }
    }
}
