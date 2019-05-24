using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LevelScoreBackend
{
    public class AngularRoutingMiddleware
    {
        private readonly RequestDelegate _next;

        public AngularRoutingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            await _next.Invoke(context);

            if (context.Response.StatusCode == 404
                && !context.Request.Path.Value.StartsWith("/api")
                && !context.Request.Path.Value.StartsWith("/updates"))
            {
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
