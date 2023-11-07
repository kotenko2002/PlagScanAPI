using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using PlagScanAPI.Entities.User;
using PlagScanAPI.Infrastructure.Configuration;
using PlagScanAPI.Infrastructure.Exceptions;
using PlagScanAPI.Models.Response;
using PlagScanAPI.Services.Authorization.Descriptors;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PlagScanAPI.Services.Authorization
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly JwtOptions _jwtOptions;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            IOptions<JwtOptions> jwtOptions)
        {
            _userManager = userManager;
            _jwtOptions = jwtOptions.Value;
        }

        public async Task<TokensModel> Login(LoginDescriptor descriptor)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(descriptor.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, descriptor.Password))
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.Unauthorized, "Wrong username or password");
            }

            IList<string> userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };
            authClaims.AddRange(userRoles.Select(role => new Claim(ClaimTypes.Role, role)));

            JwtSecurityToken accessToken = GenerateAccessToken(authClaims);
            string refreshToken = GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(_jwtOptions.RefreshTokenValidityInDays);
            await _userManager.UpdateAsync(user);

            return new TokensModel(accessToken, user);
        }

        public async Task Register(RegisterDescriptor descriptor)
        {
            ApplicationUser existsUser = await _userManager.FindByNameAsync(descriptor.Username);
            if (existsUser != null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.Conflict, "User already exists!");
            }

            var user = new ApplicationUser()
            {
                Email = descriptor.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = descriptor.Username
            };

            IdentityResult result = await _userManager.CreateAsync(user, descriptor.Password);
            if (!result.Succeeded)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.InternalServerError, "User creation failed!");
            }

            await _userManager.AddToRoleAsync(user, UserRoles.User);
        }

        public async Task<TokensModel> RefreshToken(RefreshTokenDescriptor descriptor)
        {
            string accessToken = descriptor.AccessToken;
            string refreshToken = descriptor.RefreshToken;

            ClaimsPrincipal principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Invalid access token or refresh token");
            }

            string username = principal.Identity.Name;
            ApplicationUser user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Invalid access token or refresh token");
            }

            JwtSecurityToken newAccessToken = GenerateAccessToken(principal.Claims.ToList());
            string newRefreshToken = GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(_jwtOptions.RefreshTokenValidityInDays);
            await _userManager.UpdateAsync(user);

            return new TokensModel(newAccessToken, user);
        }

        public async Task Revoke(string username)
        {
            ApplicationUser user = await _userManager.FindByNameAsync(username);
            if (user == null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Invalid access token");
            }

            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);
        }

        private JwtSecurityToken GenerateAccessToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));

            return new JwtSecurityToken(
                issuer: _jwtOptions.ValidIssuer,
                audience: _jwtOptions.ValidAudience,
                expires: DateTime.Now.AddMinutes(_jwtOptions.TokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret)),
                ValidateLifetime = false
            };

            ClaimsPrincipal principal = new JwtSecurityTokenHandler()
                .ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }

            return principal;
        }
    }
}
