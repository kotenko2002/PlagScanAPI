using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlagScanAPI.Infrastructure.Extensions;
using PlagScanAPI.Models.Request;
using PlagScanAPI.Services.Authorization;
using PlagScanAPI.Services.Authorization.Descriptors;

namespace PlagScanAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthController(
            IAuthService authService,
            IHttpContextAccessor httpContextAccessor)
        {
            _authService = authService;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var tokens = await _authService.Login(new LoginDescriptor() {
                Username = model.Username,
                Password = model.Password
            });

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
        [Route("revoke")]
        public async Task<IActionResult> Revoke()
        {
            var username = _httpContextAccessor.HttpContext.User.GetUsername();
            await _authService.Revoke(username);

            return Ok();
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
