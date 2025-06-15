using System.Security.Claims;

namespace HomeFuBack.Helpers
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal principal)
        {
            if (principal == null)
                throw new ArgumentNullException(nameof(principal));

            // Попробуйте найти claim по NameIdentifier (стандартный для ASP.NET Core Identity)
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                // Если NameIdentifier не найден, попробуйте "sub" (стандартный для JWT)
                userIdClaim = principal.FindFirst("sub"); // JwtRegisteredClaimNames.Sub
            }

            if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return userId;
            }

            // Можно выбросить исключение или вернуть Guid.Empty, если ID не найден или неверного формата
            throw new InvalidOperationException("User ID claim not found or not in GUID format.");
        }
    }
}
