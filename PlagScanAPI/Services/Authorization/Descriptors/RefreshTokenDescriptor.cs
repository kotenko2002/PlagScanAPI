namespace PlagScanAPI.Services.Authorization.Descriptors
{
    public class RefreshTokenDescriptor
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
    }
}
