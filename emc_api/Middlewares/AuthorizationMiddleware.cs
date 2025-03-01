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
        private readonly IUserRepository _userRepository;

        public HeaderAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            IUserRepository userRepository)
            : base(options, logger, encoder, clock)
        {
            _userRepository = userRepository;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var username = Request.Headers["username"].ToString();
            var machinename = Request.Headers["machinename"].ToString();

            // if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(machinename))
            // {
            //     Console.WriteLine(username);
            //     Console.WriteLine(machinename);
            //     return AuthenticateResult.Fail("Missing username or machinename header.");
            // }

            var user =await _userRepository.GetUserAsync(username, machinename);
            

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, user?.Role??"client")
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return AuthenticateResult.Success(ticket);
        }
    }
}