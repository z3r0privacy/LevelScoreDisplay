using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LevelScoreBackend.AAA;
using LevelScoreBackend.Controllers;
using LevelScoreBackend.Middleware;
using LevelScoreBackend.SignalR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LevelScoreBackend
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddScoped<CustomCookieAuthenticationEvents>();
            services.AddScoped<ILoginProvider, PasswordBasedLoginProvider>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
            services.AddSignalR();

            services.Configure<CookiePolicyOptions>(options =>
            {
                //configure some cookie stuff
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.Strict;
            });

            //services.AddAuthentication(options =>
            //{
            //    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //})
            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options => {
                    options.LoginPath = "/Login";
                    options.Cookie.Name = "LevelScoreDisplay.Admin.Cookie";
                    options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.Always;
                    options.ExpireTimeSpan = TimeSpan.FromHours(2);
                    options.LogoutPath = "/";
                    options.AccessDeniedPath = "/AccessDenied";

                    options.EventsType = typeof(CustomCookieAuthenticationEvents);

                    options.Validate();
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("Admin", policy => policy.RequireClaim(PasswordBasedLoginProvider.IS_ADMIN_CLAIM_TYPE, "yes"));
            });

            //services.AddAuthentication("BasicAuthentication")
            //    .AddScheme<AuthenticationSchemeOptions, BasicAuthenticationHandler>("BasicAuthentication", null);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseMvc();
            app.UseSignalR(route =>
            {
                route.MapHub<LevelScoreHub>("/updates");
            });

            //app.UseMiddleware<AngularRoutingMiddleware>();
            app.UseAngularRouting();

            app.UseDefaultFiles();
            app.UseStaticFiles();
        }
    }
}
