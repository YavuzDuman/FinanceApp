using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Shared.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// JWT token'dan userId'yi çıkarır. Önce "sub" claim'ini, yoksa NameIdentifier'ı kontrol eder.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>User ID (int)</returns>
        /// <exception cref="InvalidOperationException">User ID bulunamazsa fırlatılır</exception>
        public static int GetUserId(this HttpContext context)
        {
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new InvalidOperationException("User ID not found in token.");
            }

            return userId;
        }

        /// <summary>
        /// JWT token'dan userId'yi çıkarır. Önce "sub" claim'ini, yoksa NameIdentifier'ı kontrol eder.
        /// </summary>
        /// <param name="user">ClaimsPrincipal</param>
        /// <returns>User ID (int)</returns>
        /// <exception cref="InvalidOperationException">User ID bulunamazsa fırlatılır</exception>
        public static int GetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("sub") ?? 
                             user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                throw new InvalidOperationException("User ID not found in token.");
            }

            return userId;
        }

        /// <summary>
        /// JWT token'dan userId'yi güvenli şekilde çıkarır. Bulunamazsa null döner.
        /// </summary>
        /// <param name="context">HTTP context</param>
        /// <returns>User ID (int?) veya null</returns>
        public static int? TryGetUserId(this HttpContext context)
        {
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }

        /// <summary>
        /// JWT token'dan userId'yi güvenli şekilde çıkarır. Bulunamazsa null döner.
        /// </summary>
        /// <param name="user">ClaimsPrincipal</param>
        /// <returns>User ID (int?) veya null</returns>
        public static int? TryGetUserId(this ClaimsPrincipal user)
        {
            var userIdClaim = user.FindFirst("sub") ?? 
                             user.FindFirst(ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            return null;
        }
    }
}
