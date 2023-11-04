using PlagScanAPI.Entities;
using PlagScanAPI.Models.Response;
using PlagScanAPI.Services.Authorization.Descriptors;

namespace PlagScanAPI.Services.Authorization
{
    public interface IAuthService
    {
        Task<TokensModel> Login(LoginDescriptor descriptor);
        Task Register(RegisterDescriptor descriptor);
        Task<TokensModel> RefreshToken(RefreshTokenDescriptor descriptor);
        Task Revoke(string username);
    }
}
