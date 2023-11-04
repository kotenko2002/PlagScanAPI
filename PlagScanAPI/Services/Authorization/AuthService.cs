using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using PlagScanAPI.Entities;
using PlagScanAPI.Infrastructure;
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
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        public async Task<TokensModel> Login(LoginDescriptor descriptor)
        {
            var user = await _userManager.FindByNameAsync(descriptor.Username);
            if (user == null || !await _userManager.CheckPasswordAsync(user, descriptor.Password))
            {
                return null;
            }

            var userRoles = await _userManager.GetRolesAsync(user);

            var authClaims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            };

            foreach (var userRole in userRoles)
            {
                authClaims.Add(new Claim(ClaimTypes.Role, userRole));
            }

            var accessToken = GenerateAccessToken(authClaims);
            var refreshToken = GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);

            await _userManager.UpdateAsync(user);

            return new TokensModel()
            {
                AccessToken = new TokenResponseModel()
                {
                    Value = new JwtSecurityTokenHandler().WriteToken(accessToken),
                    ExpirationDate = accessToken.ValidTo
                },
                RefreshToken = new TokenResponseModel()
                {
                    Value = user.RefreshToken,
                    ExpirationDate = user.RefreshTokenExpiryTime
                }
            };
        }

        public async Task Register(RegisterDescriptor descriptor)
        {
            var existsUser = await _userManager.FindByNameAsync(descriptor.Username);
            if (existsUser != null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.InternalServerError, "User already exists!");
            }

            ApplicationUser user = new()
            {
                Email = descriptor.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = descriptor.Username
            };

            var result = await _userManager.CreateAsync(user, descriptor.Password);
            if (!result.Succeeded)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.InternalServerError, "User creation failed! Please check user details and try again.");
            }

            await _userManager.AddToRoleAsync(user, UserRoles.User);
        }

        public async Task<TokensModel> RefreshToken(RefreshTokenDescriptor descriptor)
        {
            string? accessToken = descriptor.AccessToken;
            string? refreshToken = descriptor.RefreshToken;

            var principal = GetPrincipalFromExpiredToken(accessToken);
            if (principal == null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Invalid access token or refresh token");
            }

            string username = principal.Identity.Name;
            var user = await _userManager.FindByNameAsync(username);

            if (user == null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.Now)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.BadRequest, "Invalid access token or refresh token");
            }

            var newAccessToken = GenerateAccessToken(principal.Claims.ToList());
            var newRefreshToken = GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.Now.AddDays(refreshTokenValidityInDays);

            await _userManager.UpdateAsync(user);

            return new TokensModel()
            {
                AccessToken = new TokenResponseModel()
                    {
                        Value = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                        ExpirationDate = newAccessToken.ValidTo
                    },
                RefreshToken = new TokenResponseModel()
                    {
                        Value = user.RefreshToken,
                        ExpirationDate = user.RefreshTokenExpiryTime
                    }
            };
        }

        private JwtSecurityToken GenerateAccessToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
            _ = int.TryParse(_configuration["JWT:TokenValidityInMinutes"], out int tokenValidityInMinutes);

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires: DateTime.Now.AddMinutes(tokenValidityInMinutes),
                claims: authClaims,
                signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
            );

            return token;
        }
        private string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
