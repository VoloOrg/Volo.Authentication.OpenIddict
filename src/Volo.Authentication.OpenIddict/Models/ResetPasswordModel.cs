﻿namespace Volo.Authentication.OpenIddict.Models
{
    public class ResetPasswordModel
    {
        public string NewPassword { get; set; } = default!;
        public string ConfirmPassword { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string Token { get; set; } = default!;
    }
}
