using PlagScanAPI.Entities.User;
using System.IdentityModel.Tokens.Jwt;

namespace PlagScanAPI.Models.Response
{
    public class TokensModel
    {
        public TokenResponseModel AccessToken { get; set; }
        public TokenResponseModel RefreshToken { get; set; }

        public TokensModel(JwtSecurityToken accessToken, ApplicationUser user)
        {
            AccessToken = new TokenResponseModel()
            {
                Value = new JwtSecurityTokenHandler().WriteToken(accessToken),
                ExpirationDate = accessToken.ValidTo
            };

            RefreshToken = new TokenResponseModel()
            {
                Value = user.RefreshToken,
                ExpirationDate = user.RefreshTokenExpiryTime
            };
        }
    }
}
