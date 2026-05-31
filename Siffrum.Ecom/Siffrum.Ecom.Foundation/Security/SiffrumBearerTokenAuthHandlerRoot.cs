using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Siffrum.Ecom.DAL.Context;
using Siffrum.Ecom.ServiceModels.Foundation.Base.CommonResponseRoot;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;

namespace Siffrum.Ecom.Foundation.Security
{
    public class SiffrumBearerTokenAuthHandlerRoot : AuthenticationHandler<SiffrumAuthenticationSchemeOptions>
    {
        private string _failureMsg = string.Empty;
        private readonly JwtHandler _jwtHandler;

        public const string DefaultSchema = "CodeVisionBearerSchema";

        public SiffrumBearerTokenAuthHandlerRoot(IOptionsMonitor<SiffrumAuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock, JwtHandler jwtHandler)
            : base(options, logger, encoder, clock)
        {
            _jwtHandler = jwtHandler;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            try
            {
                string tokenString = GetRequestBearerTokenValue("Authorization");

                // SignalR WebSocket connections cannot send custom headers during handshake.
                // Fall back to query string token for hub paths.
                if (string.IsNullOrEmpty(tokenString))
                {
                    var accessToken = Request.Query["access_token"].FirstOrDefault();
                    var path = Context.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                    {
                        tokenString = accessToken;
                    }
                }

                if (!string.IsNullOrEmpty(tokenString))
                {
                    AuthenticationTicket authTicket = await ValidateTokenAndGetTicket(tokenString);
                    if (authTicket != null)
                    {
                        DateTimeOffset utcNow = Clock.UtcNow;
                        DateTimeOffset? expiresUtc = authTicket.Properties.ExpiresUtc;
                        if (utcNow > expiresUtc)
                        {
                            return GetFailureResult("Token is expired.");
                        }

                        // Single-device enforcement for User and DeliveryBoy
                        var stampCheckResult = await ValidateSecurityStampAsync(authTicket.Principal);
                        if (stampCheckResult != null)
                        {
                            return stampCheckResult;
                        }

                        return AuthenticateResult.Success(authTicket);
                    }

                    return GetFailureResult("Could not unprotect token");
                }

                return GetFailureResult("Token is null or empty.");
            }
            catch (Exception)
            {
                return GetFailureResult("Could not unprotect token");
            }
        }

        protected string GetRequestBearerTokenValue(string key)
        {
            if (Request.Headers.TryGetValue(key, out var value))
            {
                string text = value.ToString();
                return text.Split(' ').Count() > 1 ? text.Split(' ')[1] : "";
            }

            return null;
        }

        protected override Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            return base.HandleForbiddenAsync(properties).ContinueWith(delegate
            {
                if (!string.IsNullOrWhiteSpace(_failureMsg))
                {
                    Response.Body.WriteAsync(Encoding.ASCII.GetBytes(_failureMsg));
                }
            });
        }

        protected override Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            return base.HandleChallengeAsync(properties).ContinueWith(delegate
            {
                if (!string.IsNullOrWhiteSpace(_failureMsg))
                {
                    Response.Body.WriteAsync(Encoding.ASCII.GetBytes(_failureMsg));
                }
            });
        }

        protected async Task<AuthenticationTicket> ValidateTokenAndGetTicket(string tokenString)
        {
            JwtSecurityToken jwtAuthTicket = await _jwtHandler.UnprotectAsync(OptionsMonitor.CurrentValue.JwtTokenSigningKey, tokenString);
            if (jwtAuthTicket != null)
            {
                AuthenticationProperties authProps = new AuthenticationProperties
                {
                    IssuedUtc = jwtAuthTicket.ValidFrom.ToUniversalTime(),
                    ExpiresUtc = jwtAuthTicket.ValidTo.ToUniversalTime()
                };
                ClaimsIdentity claimId = new ClaimsIdentity(jwtAuthTicket.Claims, "Bearer");
                ClaimsPrincipal claimPrin = new ClaimsPrincipal(claimId);
                return new AuthenticationTicket(claimPrin, authProps, "CodeVisionBearerSchema");
            }
            
            return null;
        }

        protected AuthenticateResult GetFailureResult(string message)
        {
            AuthenticationProperties properties = new AuthenticationProperties();
            _failureMsg = message;
            return AuthenticateResult.Fail(message, properties);
        }

        /// <summary>
        /// Validates the SecurityStamp in the JWT against the DB for User and DeliveryBoy roles.
        /// Returns null if valid (or not applicable), or an AuthenticateResult.Fail if stamp mismatch.
        /// </summary>
        private async Task<AuthenticateResult?> ValidateSecurityStampAsync(ClaimsPrincipal principal)
        {
            // Only enforce for User and DeliveryBoy
            if (!principal.IsInRole("User") && !principal.IsInRole("DeliveryBoy"))
                return null;

            var stampClaim = principal.FindFirst(DomainConstants.ClaimsRoot.Claim_SecurityStamp)?.Value;

            // Tokens issued before this feature won't have a stamp — allow them through
            if (string.IsNullOrEmpty(stampClaim))
                return null;

            var dbRecordIdClaim = principal.FindFirst(DomainConstants.ClaimsRoot.Claim_DbRecordId)?.Value;
            if (string.IsNullOrEmpty(dbRecordIdClaim) || !long.TryParse(dbRecordIdClaim, out var userId))
                return null;

            try
            {
                using var scope = Context.RequestServices.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApiDbContext>();

                string? dbStamp = null;
                if (principal.IsInRole("User"))
                {
                    dbStamp = await dbContext.User
                        .Where(u => u.Id == userId)
                        .Select(u => u.SecurityStamp)
                        .FirstOrDefaultAsync();
                }
                else if (principal.IsInRole("DeliveryBoy"))
                {
                    dbStamp = await dbContext.DeliveryBoy
                        .Where(d => d.Id == userId)
                        .Select(d => d.SecurityStamp)
                        .FirstOrDefaultAsync();
                }

                if (!string.IsNullOrEmpty(dbStamp) && dbStamp != stampClaim)
                {
                    return GetFailureResult("Session expired. You have been logged in on another device.");
                }
            }
            catch
            {
                // If DB is unreachable, don't block auth — fail open
            }

            return null;
        }
    }
}

