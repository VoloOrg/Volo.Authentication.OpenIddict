﻿using Newtonsoft.Json.Linq;
using AuthenticationProject.API.Options;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AuthenticationProject.API.Models;
using Newtonsoft.Json;

namespace AuthenticationProject.API.Middlewares
{
    public class AddTokenAsCookieMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly AuthenticationOptions _authenticationOptions;
        private readonly AppInfo _appInfoOptions;

        public AddTokenAsCookieMiddleware(RequestDelegate next, IOptions<AuthenticationOptions> authenticationOptions, IOptions<AppInfo> appInfoOptions)
        {
            _authenticationOptions = authenticationOptions.Value;
            _appInfoOptions = appInfoOptions.Value;
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path == "/auth/connect/token")
            {
                HttpClient client = new HttpClient();

                var formData = await context.Request.ReadFormAsync();

                Dictionary<string, string> parameters = formData.Select(s => new KeyValuePair<string, string>(s.Key, s.Value.ToString())).ToDictionary(l => l.Key, l => l.Value);

                parameters.Add("grant_type", "password");
                parameters.Add("scope", _appInfoOptions.Scope);
                parameters.Add("client_id", _appInfoOptions.ClientId);
                parameters.Add("client_secret", _appInfoOptions.ClientSecret);


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

                    var responceJson = JObject.Parse(responseBodyString);

                    var token = responceJson["access_token"].ToString();
                    var refreshToken = responceJson["refresh_token"].ToString();

                    DeleteCookies(context.Response);
                    AppendCookies(context.Response, token, refreshToken);

                    await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }
                
            }
            else if(context.Request.Path == "/auth/connect/logout" )
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
                    await GenerateResponse(context.Response, string.Empty, 200, string.Empty);
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, 403, await response.Content.ReadAsStringAsync());
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
                else
                {
                    await GenerateResponse(context.Response, string.Empty, 403, await response.Content.ReadAsStringAsync());
                }
            }
            else if(context.Request.Path == "/auth/account/ForgotPassword"
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
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/ForgotPassword"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    var responseObject = JsonConvert.DeserializeObject<ForgotPasswordResponseModel>(await response.Content.ReadAsStringAsync());
                    
                    await GenerateResponse(context.Response, responseObject, 200, string.Empty);
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
                }
            }
            else if (context.Request.Path == "/auth/account/ResetPassword"
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
                    RequestUri = new Uri(_authenticationOptions.AuthenticationUrl + "account/ResetPassword"),
                    Method = new(HttpMethods.Post),
                    Content = new StringContent(requestBodyString, Encoding.UTF8, "application/json"),
                };

                var response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    await GenerateResponse(context.Response, string.Empty, 200, "Password reseted successfully");
                }
                else
                {
                    await GenerateResponse(context.Response, string.Empty, (int)response.StatusCode, await response.Content.ReadAsStringAsync());
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

                        var responceJson = JObject.Parse(responseBodyString);

                        var accessToken = responceJson["access_token"].ToString();
                        var refreshToken = responceJson["refresh_token"].ToString();

                        DeleteCookies(context.Response);
                        AppendCookies(context.Response, token, refreshToken);

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

            var json = JsonConvert.SerializeObject(result);
            var bytes = Encoding.UTF8.GetBytes(json);
            await httpResponse.BodyWriter.WriteAsync(bytes);
        }

        #endregion
    }
}
