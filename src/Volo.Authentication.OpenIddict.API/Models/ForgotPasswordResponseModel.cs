﻿namespace Volo.Authentication.OpenIddict.API.Models
{
    public class ForgotPasswordResponseModel
    {
        public string Token { get; set; }
        public string Email { get; set; }
        public string Type { get; set; }
    }
}
