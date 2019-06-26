using LevelScoreBackend.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.AAA
{
    public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
    {
        public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            var data = context.Principal.Claims.FirstOrDefault(c => c.Type == PasswordBasedLoginProvider.SERVER_START_TIME_CLAIM_TYPE);
            if (!long.TryParse(data?.Value, out var dt) || dt != Program.StartTime)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }

        public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.Headers["WWW-Authenticate"] = "Cookie realm=\"Administration page\"";
            return Task.CompletedTask;
        }

        public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
    }
}
