using System.ComponentModel.DataAnnotations;

namespace AuthenticationOpenIddict.Models
{
    public class ForgotPasswordEmailModel
    {
        [EmailAddress]
        public string Email { get; set; }
    }
}
