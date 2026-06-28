using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CleanArc.Infrastructure.Identity.Identity.Dtos;

namespace CleanArc.Web.Api;

public class HangfireAdminAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();

        // 1. Try to get the token from Cookie, Query string, or Authorization header
        string? token = null;

        // Try Query parameter "access_token" or "token"
        if (httpContext.Request.Query.TryGetValue("access_token", out var accessTokenValues))
        {
            token = accessTokenValues.FirstOrDefault();
        }
        else if (httpContext.Request.Query.TryGetValue("token", out var tokenValues))
        {
            token = tokenValues.FirstOrDefault();
        }
        // Try Cookie "hangfireToken"
        else if (httpContext.Request.Cookies.TryGetValue("hangfireToken", out var cookieValue))
        {
            token = cookieValue;
        }
        // Try Authorization header
        else
        {
            var authHeader = httpContext.Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                token = authHeader.Substring("Bearer ".Length).Trim();
            }
        }

        if (string.IsNullOrEmpty(token))
        {
            // If User is already authenticated (e.g. via cookie auth or other means in the pipeline)
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                return CheckAdminRole(httpContext.User);
            }
            return false;
        }

        // Validate the JWT Token
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var identitySettings = httpContext.RequestServices.GetRequiredService<IOptions<IdentitySettings>>().Value;
            var secretKey = Encoding.UTF8.GetBytes(identitySettings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateIssuer = true,
                ValidIssuer = identitySettings.Issuer,
                ValidateAudience = true,
                ValidAudience = identitySettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var _);
            
            // Set httpContext.User so downstream handlers or layout might see it (optional)
            httpContext.User = principal;

            return CheckAdminRole(principal);
        }
        catch
        {
            return false;
        }
    }

    private bool CheckAdminRole(ClaimsPrincipal principal)
    {
        // Require role admin/Admin/InstitutionAdmin/institutionadmin or similar
        return principal.IsInRole("Admin") || 
               principal.IsInRole("admin") || 
               principal.IsInRole("InstitutionAdmin") || 
               principal.IsInRole("institutionadmin");
    }
}
