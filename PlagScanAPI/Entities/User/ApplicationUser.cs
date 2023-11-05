using Microsoft.AspNetCore.Identity;

namespace PlagScanAPI.Entities.User
{
    public class ApplicationUser : IdentityUser
    {
        public string RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
