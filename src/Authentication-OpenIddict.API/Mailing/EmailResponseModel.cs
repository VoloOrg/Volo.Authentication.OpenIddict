﻿using System.Net;

namespace AuthenticationOpenIddict.API.Mailing
{
    public class EmailResponseModel
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
