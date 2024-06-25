using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Identity.Web;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;

namespace BFF_Web_App
{
    internal static class AuthServiceCollectionExtensions
    {
        internal static IServiceCollection AddEntraAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            string[] initialScopes = configuration.GetSection("DownstreamApis:MicrosoftGraph:Scopes").Get<string[]>();

            services.AddAuthentication("MicrosoftOidc")
                    .AddMicrosoftIdentityWebApp(configuration.GetSection("AzureAd"), "MicrosoftOidc")
                    .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
                    .AddMicrosoftGraph(configuration.GetSection("DownstreamApi"))
                    .AddDownstreamApi("MyApi", configuration.GetSection("DownstreamApis:DownstreamApi"))
                    .AddInMemoryTokenCaches();

            services.ConfigureCookieOidcRefresh(CookieAuthenticationDefaults.AuthenticationScheme, "MicrosoftOidc");
            services.Configure<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme, options => options.Events = new RejectSessionCookieWhenAccountNotInCacheEvents(configuration));
            services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

            return services;
        }

        internal static IServiceCollection AddOIDCAuthentication(this IServiceCollection services,
       IConfiguration configuration)
        {
            string[] initialScopes = configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');
            services.AddAuthentication("MicrosoftOidc")
                .AddOpenIdConnect("MicrosoftOidc", oidcOptions =>
                {
                    oidcOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    oidcOptions.CallbackPath = new PathString("/signin-oidc");

                    oidcOptions.Scope.Add(initialScopes[0]);
                    oidcOptions.Scope.Add(initialScopes[1]);
                    oidcOptions.Authority = $"https://login.microsoftonline.com/{configuration["AzureAd:TenantId"]}/v2.0/";

                    oidcOptions.ClientId = configuration["AzureAd:ClientId"];
                    oidcOptions.ClientSecret = configuration["AzureAd:ClientSecret"];

                    oidcOptions.ResponseType = OpenIdConnectResponseType.Code;
                    oidcOptions.MapInboundClaims = false;
                    oidcOptions.TokenValidationParameters.NameClaimType = JwtRegisteredClaimNames.Name;
                    oidcOptions.TokenValidationParameters.RoleClaimType = "role";
                })
                .AddCookie("Cookies");

            services.ConfigureCookieOidcRefresh("Cookies", "MicrosoftOidc");

            services.AddScoped<AuthenticationStateProvider, PersistingAuthenticationStateProvider>();

            return services;

        }
    }
}