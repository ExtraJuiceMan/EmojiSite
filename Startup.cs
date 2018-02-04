using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using EmojiSite.Models;
using EmojiSite.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using PaulMiami.AspNetCore.Mvc.Recaptcha;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.HttpOverrides;

namespace EmojiSite
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<EmojiContext>(options =>
                options.UseMySql(Configuration["CString"])
                );

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddRecaptcha(new RecaptchaOptions
            {
                SiteKey = Configuration["Recaptcha:SiteKey"],
                SecretKey = Configuration["Recaptcha:SecretKey"]
            });

            services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = "Discord";
            })
             .AddCookie()
             .AddOAuth("Discord", x =>
             {
                 x.ClientId = Configuration["Discord:AppId"];
                 x.ClientSecret = Configuration["Discord:AppSecret"];

                 x.AuthorizationEndpoint = "https://discordapp.com/api/oauth2/authorize";
                 x.TokenEndpoint = "https://discordapp.com/api/oauth2/token";
                 x.UserInformationEndpoint = "https://discordapp.com/api/users/@me";

                 x.ClaimActions.MapJsonKey(ClaimTypes.NameIdentifier, "id", ClaimValueTypes.UInteger64);
                 x.ClaimActions.MapJsonKey(ClaimTypes.Name, "username", ClaimValueTypes.String);
                 x.ClaimActions.MapJsonKey(ClaimTypes.Email, "email", ClaimValueTypes.Email);
                 x.ClaimActions.MapJsonKey("urn:discord:discriminator", "discriminator", ClaimValueTypes.UInteger32);
                 x.ClaimActions.MapJsonKey("urn:discord:avatar", "avatar", ClaimValueTypes.String);
                 x.ClaimActions.MapJsonKey("urn:discord:verified", "verified", ClaimValueTypes.Boolean);

                 x.Events = new Microsoft.AspNetCore.Authentication.OAuth.OAuthEvents
                 {
                     OnCreatingTicket = async c =>
                     {
                         var request = new HttpRequestMessage(HttpMethod.Get, c.Options.UserInformationEndpoint);
                         request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                         request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", c.AccessToken);

                         var response = await c.Backchannel.SendAsync(request, c.HttpContext.RequestAborted);
                         response.EnsureSuccessStatusCode();

                         var user = JObject.Parse(await response.Content.ReadAsStringAsync());
                         c.RunClaimActions(user);
                     }
                 };

                 x.CallbackPath = new Microsoft.AspNetCore.Http.PathString("/sign-in");
                 x.SaveTokens = true;
                 x.Scope.Add("identify");
             });

            services.AddOptions();
            services.AddMemoryCache();

            services.Configure<IpRateLimitOptions>(Configuration.GetSection("IpRateLimiting"));
            services.Configure<IpRateLimitPolicies>(Configuration.GetSection("IpRateLimitPolicies"));

            services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
            services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();

            services.AddMvc();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseBrowserLink();
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseStatusCodePages();
            app.UseIpRateLimiting();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
