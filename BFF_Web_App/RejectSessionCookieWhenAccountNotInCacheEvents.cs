using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Graph.Models.ExternalConnectors;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace BFF_Web_App
{
    internal class RejectSessionCookieWhenAccountNotInCacheEvents : CookieAuthenticationEvents
    {
        private readonly IConfiguration _configuration;

        public RejectSessionCookieWhenAccountNotInCacheEvents(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async override Task ValidatePrincipal(CookieValidatePrincipalContext context)
        {
            string[] scopes = _configuration.GetSection("DownstreamApis:WeatherForecast:Scopes").Get<string[]>();

            try
            {
                var tokenAcquisition = context.HttpContext.RequestServices.GetRequiredService<ITokenAcquisition>();
                string token = await tokenAcquisition.GetAccessTokenForUserAsync(
                    scopes: scopes!,
                    user: context.Principal);
            }
            catch (MicrosoftIdentityWebChallengeUserException ex)
               when (AccountDoesNotExitInTokenCache(ex))
            {
                context.RejectPrincipal();
            }
        }

        /// <summary>
        /// Is the exception thrown because there is no account in the token cache?
        /// </summary>
        /// <param name="ex">Exception thrown by <see cref="ITokenAcquisition"/>.GetTokenForXX methods.</param>
        /// <returns>A boolean telling if the exception was about not having an account in the cache</returns>
        private static bool AccountDoesNotExitInTokenCache(MicrosoftIdentityWebChallengeUserException ex)
        {
            return ex.InnerException is MsalUiRequiredException
                                      && (ex.InnerException as MsalUiRequiredException).ErrorCode == "user_null";
        }
    }
}