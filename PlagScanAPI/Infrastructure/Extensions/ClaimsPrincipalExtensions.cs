using PlagScanAPI.Infrastructure.Exceptions;
using System.Net;
using System.Security.Claims;

namespace PlagScanAPI.Infrastructure.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static string GetUsername(this ClaimsPrincipal principal)
        {
            return GetInfoByDataName(principal, ClaimTypes.Name);
        }

        private static string GetInfoByDataName(ClaimsPrincipal principal, string name)
        {
            var data = principal.FindFirstValue(name);

            if (data == null)
            {
                throw new ExceptionWithStatusCode(HttpStatusCode.InternalServerError, $"No such data as {name} in Token");
            }

            return data;
        }
    }
}
