using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend.Middleware
{
    public class AngularRoutingMiddleware
    {
        private readonly RequestDelegate _next;

        public AngularRoutingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IAuthenticationService authService)
        {
            await _next.Invoke(context);

            if (context.Response.StatusCode == 404
                && !context.Request.Path.Value.StartsWith("/api")
                && !context.Request.Path.Value.StartsWith("/updates"))
            {
                if (context.Request.Path.Value.ToLower().StartsWith("/admin"))
                {
                    var authRes = await authService.AuthenticateAsync(context, CookieAuthenticationDefaults.AuthenticationScheme);
                    if (!authRes.Succeeded)
                    {
                        context.Response.StatusCode = 301;
                        context.Response.Headers.Add("Location", "/login");
                        return;
                    }
                }
                context.Request.Path = new PathString("/index.html");
                await _next.Invoke(context);
            }
        }
    }

    public static class AngularRoutingMiddlewareExtensions
    {
        public static IApplicationBuilder UseAngularRouting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AngularRoutingMiddleware>();
        }
    }
}
