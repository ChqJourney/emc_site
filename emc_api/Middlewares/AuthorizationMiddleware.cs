using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using emc_api.Repositories;

namespace emc_api.Middleware
{
    public class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
    private readonly IAuthService _authService;
        public HeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IAuthService authService)
            : base(options, logger, encoder, clock)
        {
            _authService = authService;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var username = Request.Headers["username"].ToString();
            var password = Request.Headers["password"].ToString();
            
        //   if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        // {
        //     return AuthenticateResult.Fail("Missing username or password header.");
        // }

        // var user = await _authService.ValidateUserAsync(username, password);
        
        // if (user == null)
        // {
        //     return AuthenticateResult.Fail("Invalid username or password.");
        // }

            

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "client")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}