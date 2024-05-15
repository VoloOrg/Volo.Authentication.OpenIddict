using System.ComponentModel.DataAnnotations;

namespace Volo.Authentication.OpenIddict.Models
{
    public class ForgotPasswordEmailModel
    {
        [EmailAddress]
        public string Email { get; set; }
    }
}
