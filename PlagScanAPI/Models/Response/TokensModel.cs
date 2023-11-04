namespace PlagScanAPI.Models.Response
{
    public class TokensModel
    {
        public TokenResponseModel AccessToken { get; set; }
        public TokenResponseModel RefreshToken { get; set; }
    }
}
