using Volo.Authentication.OpenIddict.API.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Volo.Authentication.OpenIddict.API.Models;
using Volo.Authentication.OpenIddict.API.Mailing;
using System.Text.Json.Nodes;
using System.Text.Json;

namespace Volo.Authentication.OpenIddict.API.Middlewares
{
    public class AddTokenAsCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly AppInfoOptions _appInfoOptions;
        private readonly MailingOptions _mailOptions;
        private readonly IMailingService _mailingService;

        public AddTokenAsCookieMiddleware(
            RequestDelegate next, 
            IOptions<AuthenticationOptions> authenticationOptions, 
            IOptions<AppInfoOptions> appInfoOptions, 
            IOptions<MailingOptions> mailOptions, 
            IMailingService mailingService)
        {
            _authenticationOptions = authenticationOptions.Value;
            _appInfoOptions = appInfoOptions.Value;
            _mailOptions = mailOptions.Value;
            _mailingService = mailingService;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/auth/connect/token")
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                var body = JsonSerializer.Deserialize<LoginModel>(requestBodyString, new JsonSerializerOptions() {PropertyNameCaseInsensitive = true });

                Dictionary<string, string> parameters = new()
                {
                    { "username", body.Email },
                    { "password", body.Password },
                    { "grant_type", "password" },
                    { "scope", _appInfoOptions.Scope },
                    { "client_id", _appInfoOptions.ClientId },
                    { "client_secret", _appInfoOptions.ClientSecret }
                };


                HttpRequestMessage request = new()
                { 
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/token"), 
                    Method = new(HttpMethods.Post),
                    Content = new FormUrlEncodedContent(parameters),
                };

                var response = await client.SendAsync(request);

                
                if (response.IsSuccessStatusCode)
                {
                    string responseBodyString = await response.Content.ReadAsStringAsync();

                    var responceJson = JsonObject.Parse(responseBodyString);

                    var token = responceJson["access_token"].ToString();
                    var refreshToken = responceJson["refresh_token"].ToString();

                    DeleteCookies(context.Response);
                    AppendCookies(context.Response, token, refreshToken);

                    await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, (int)response.StatusCode, "Failed to log in");
                }
                
            }
            else if(context.Request.Path == "/auth/account/logout")
            {
                HttpClient client = new HttpClient();

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/logout"),
                    Method = new(HttpMethods.Get)
                };


                if (context.Request.Cookies.TryGetValue("access_token", out var tokenForLogout)
                && !string.IsNullOrWhiteSpace(tokenForLogout))
                {
                    request.Headers.Remove("Authorization");
                    request.Headers.Add("Authorization", "Bearer " + tokenForLogout);
                }                

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    DeleteCookies(context.Response);
                    await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
                }
                else
                {
                    DeleteCookies(context.Response);
                    await GenerateResponse(context.Response, string.Empty, 200, await response.Content.ReadAsStringAsync());
                }

                await _next(context);
            }
            else if (context.Request.Path == "/auth/account/changepassword"
                && context.Request.Cookies.TryGetValue("access_token", out var tokenForPassChange)
                && !string.IsNullOrWhiteSpace(tokenForPassChange))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/changepassword"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "Bearer " + tokenForPassChange);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, true, 200, string.Empty);
                }
                else if(response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    await GenerateResponse(context.Response, false, 400, "Incorrect information");
                }
                else
                {
                    await GenerateResponse(context.Response, false, 403, await response.Content.ReadAsStringAsync());
                }
            }
            else if (context.Request.Path == "/auth/account/register"
                && context.Request.Cookies.TryGetValue("access_token", out var tokenForRegister)
                && !string.IsNullOrWhiteSpace(tokenForRegister))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/Register"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                request.Headers.Remove("Authorization");
                request.Headers.Add("Authorization", "Bearer " + tokenForRegister);

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
                }
                else if(response.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    await GenerateResponse(context.Response, string.Empty, 400, "incorrect information");
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, 403, await response.Content.ReadAsStringAsync());
                }
            }
            else if(context.Request.Path == "/auth/connect/ForgotPassword"
                && !context.Request.Cookies.ContainsKey("access_token"))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/ForgotPassword"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    //Email or other method to send the token
                    var responseObject = JsonSerializer.Deserialize<ForgotPasswordResponseModel>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions() { PropertyNameCaseInsensitive = true});

                    string mailContent = _mailOptions.EnvironmentUri + _mailOptions.ResetEndpoint + "?" + $"token={responseObject.Token}&email={responseObject.Email}&type={responseObject.Type}";

                    var sendEmailModel = new SendEmailModel()
                    {
                        FromEmail = _mailOptions.FromEmail,
                        FromName = _mailOptions.FromName,
                        PlainTextMessage = mailContent,
                        HtmlTextMessage = string.Empty,
                        Subject = "reset password test",
                        ToEmail = responseObject.Email,
                        ToName = responseObject.Email
                    };

                    var mailResponse = await _mailingService.SendEmailAsync(sendEmailModel, CancellationToken.None);

                    //for mailing service
                    await GenerateResponse(context.Response, true, 200, "mail is sent");
                    //for development sends token and email as responce
                    //await GenerateResponse(context.Response, responseObject, 200, string.Empty);
                }
                else
                {
                    await GenerateResponse(context.Response, true, 200, "mail is sent");
                }
            }
            else if (context.Request.Path == "/auth/connect/VerifyToken"
                && !context.Request.Cookies.ContainsKey("access_token"))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/VerifyToken"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, true, 200, "All good, proceed");
                }
                else
                {
                    await GenerateResponse(context.Response, false, 200, "Not valid token");
                }
            }
            else if (context.Request.Path == "/auth/connect/ResetPassword"
                && !context.Request.Cookies.ContainsKey("access_token"))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/ResetPassword"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, true, 200, "Password reseted successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    await GenerateResponse(context.Response, false, 400, "Password/confirm password/token are wrong");
                }
                else
                {
                    await GenerateResponse(context.Response, false, 400, "Password reset failed");
                }
            }
            else if (context.Request.Path == "/auth/connect/Register"
                && !context.Request.Cookies.ContainsKey("access_token"))
            {
                HttpClient client = new HttpClient();

                string requestBodyString = null;

                using (var reader = new StreamReader(context.Request.Body))
                {
                    requestBodyString = await reader.ReadToEndAsync();
                }

                HttpRequestMessage request = new()
                {
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/Register"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, true, 200, "Password seted successfully");
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    await GenerateResponse(context.Response, false, 400, "Password/confirm password/token are wrong");
                }
                else
                {
                    await GenerateResponse(context.Response, false, 400, "Set password failed");
                }
            }
            else if (context.Request.Cookies.TryGetValue("access_token", out var token) && !string.IsNullOrWhiteSpace(token))
            {
                var handler = new JwtSecurityTokenHandler();
                var jwtSecurityToken = handler.ReadJwtToken(token);

                if(jwtSecurityToken is null || !jwtSecurityToken.Claims.Any(c => c.Type == "exp")) 
                {
                    await GenerateResponse(context.Response, string.Empty, 401, "Expieration missing");
                    return;
                }

                var tokenExpieres = DateTimeOffset.FromUnixTimeSeconds( long.Parse(jwtSecurityToken.Claims.First(t => t.Type == "exp").Value)).UtcDateTime;

                if(tokenExpieres.AddMinutes(-1) <= DateTime.UtcNow 
                    && context.Request.Cookies.TryGetValue("refresh_token", out var cookieRefreshToken) 
                    && !string.IsNullOrWhiteSpace(cookieRefreshToken))
                {
                    HttpClient client = new HttpClient();

                    var formData = new Dictionary<string, string>()
                    {
                        { "grant_type", "refresh_token" },
                        { "refresh_token", cookieRefreshToken },
                        { "scope", _appInfoOptions.Scope },
                        { "client_id", _appInfoOptions.ClientId },
                        { "client_secret", _appInfoOptions.ClientSecret }
                    }; 

                    HttpRequestMessage request = new()
                    {
                        RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "connect/token"),
                        Method = new(HttpMethods.Post),
                        Content = new FormUrlEncodedContent(formData),
                    };

                    request.Headers.Remove("Authorization");
                    request.Headers.Add("Authorization", "Bearer " + token);

                    var response = await client.SendAsync(request);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBodyString = await response.Content.ReadAsStringAsync();

                        var responceJson = JsonObject.Parse(responseBodyString);

                        var accessToken = responceJson["access_token"].ToString();
                        var refreshToken = responceJson["refresh_token"].ToString();

                        DeleteCookies(context.Response);
                        AppendCookies(context.Response, accessToken, refreshToken);

                        token = accessToken;
                    }
                    else
                    {
                        await GenerateResponse(context.Response, string.Empty, 401, await response.Content.ReadAsStringAsync());
                    }
                }

                context.Request.Headers.Remove("Authorization");
                context.Request.Headers.Add("Authorization", "Bearer " + token);
                await _next(context);
            }
            else
            {
                await GenerateResponse(context.Response, string.Empty, 401, string.Empty);
            }
        }

        #region Private Methods

        private static void DeleteCookies(HttpResponse httpResponse)
        {
            httpResponse.Cookies.Delete("access_token");
            httpResponse.Cookies.Delete("refresh_token");
        }

        private void AppendCookies(HttpResponse httpResponse, string accessToken, string refreshToken) 
        {
            //todo: samesite
            var options = new CookieOptions() { HttpOnly = true, Secure = true, IsEssential = true, SameSite = SameSiteMode.None, Expires = DateTime.UtcNow.AddMinutes(3600) };
            httpResponse.Cookies.Append("access_token", accessToken, options);
            httpResponse.Cookies.Append("refresh_token", refreshToken, options);
        }

        private static async Task GenerateResponse<T>(HttpResponse httpResponse, T? data, int status, string? message)
        {
            var result = new ResponseModel<T>();
            result.Code = status;
            result.Data = data;
            result.Message = message;

            httpResponse.StatusCode = status;
            httpResponse.ContentType = "application/json";

            var json = JsonSerializer.Serialize(result, new JsonSerializerOptions
            {
                 PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var bytes = Encoding.UTF8.GetBytes(json); 
            await httpResponse.BodyWriter.WriteAsync(bytes);
        }

        #endregion
    }
}
