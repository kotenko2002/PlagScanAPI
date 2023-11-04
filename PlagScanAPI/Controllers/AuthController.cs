using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PlagScanAPI.Entities;
using PlagScanAPI.Infrastructure;
using PlagScanAPI.Infrastructure.Exceptions;
using PlagScanAPI.Models.Request;
using PlagScanAPI.Models.Response;
using PlagScanAPI.Services.Authorization;
using PlagScanAPI.Services.Authorization.Descriptors;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PlagScanAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IAuthService _authService;
        
        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IAuthService authService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _authService = authService;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var tokens = await _authService.Login(new LoginDescriptor() {
                Username = model.Username,
                Password = model.Password
            });

            if (tokens == null)
            {
                return Unauthorized();
            }

            return Ok(tokens);
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            await _authService.Register(new RegisterDescriptor()
            {
                Username = model.Username,
                Email = model.Email,
                Password = model.Password
            });

            return Ok("User created successfully!");
        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken(RefreshTokenModel model)
        {
            var newTokens = await _authService.RefreshToken(new RefreshTokenDescriptor()
            {
                AccessToken = model.AccessToken,
                RefreshToken = model.RefreshToken
            });

            return Ok(newTokens);
        }

        [Authorize]
        [HttpPost]
        [Route("revoke/{username}")]
        public async Task<IActionResult> Revoke(string username)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null) return BadRequest("Invalid user name");

            user.RefreshToken = null;
            await _userManager.UpdateAsync(user);

            return NoContent();
        }

        [Authorize]
        [HttpGet]
        [Route("test")]
        public IActionResult TestEndpoint()
        {
            return Ok();
        }
    }
}
