using System.Net;

namespace Volo.Authentication.OpenIddict.Mailing
{
    public class EmailResponseModel
    {
        public bool IsSuccess { get; set; }
        public HttpStatusCode StatusCode { get; set; }
        public string? Message { get; set; }
    }
}
