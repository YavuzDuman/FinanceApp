using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;

namespace Shared.Helpers
{
    /// <summary>
    /// Kullanıcı context bilgilerini yöneten merkezi helper
    /// </summary>
    public static class UserContextHelper
    {
        /// <summary>
        /// Request'ten kullanıcı ID'sini alır
        /// </summary>
        public static int GetUserIdFromToken(HttpContext context)
        {
            // API Gateway'den gelen X-User-ID header'ını kullan
            if (context.Request.Headers.TryGetValue("X-User-ID", out var userIdHeader))
            {
                Console.WriteLine($"X-User-ID found: {userIdHeader}");
                if (int.TryParse(userIdHeader.FirstOrDefault(), out int userId))
                {
                    Console.WriteLine($"Parsed User ID: {userId}");
                    return userId;
                }
            }
            
            // Fallback: Authorization header'dan JWT decode et (geçici)
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                var token = authHeader.ToString().Replace("Bearer ", "");
                try
                {
                    var handler = new JwtSecurityTokenHandler();
                    var jsonToken = handler.ReadJwtToken(token);
                    var userIdClaim = jsonToken.Claims.FirstOrDefault(x => x.Type == "sub");
                    if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                    {
                        Console.WriteLine($"Fallback: User ID from JWT: {userId}");
                        return userId;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JWT decode error: {ex.Message}");
                }
            }
            
            throw new InvalidOperationException("User ID not found in request headers or JWT token.");
        }

		/// <summary>
		/// Cache'li kullanıcı ID alma
		/// </summary>
		public static async Task<int> GetUserIdFromTokenCachedAsync(HttpContext context, IMemoryCache cache)
		{
			// 1. Hızlı Cache Anahtarı Oluşturma: Raw Token'ı al
			var rawToken = context.Request.Headers["Authorization"]
									.FirstOrDefault()?
									.Replace("Bearer ", "");

			// Eğer token yoksa, Unauthorized fırlatılmalı
			if (string.IsNullOrEmpty(rawToken))
			{
				throw new InvalidOperationException("Authorization token missing.");
			}

			// Cache Anahtarı: Raw Token'ın kendisi (veya hash'i)
			var cacheKey = $"validated_user_id_{rawToken}";

			// 2. ÖNCE CACHE KONTROLÜ
			if (cache.TryGetValue(cacheKey, out int cachedUserId))
			{
				// CACHE HIT: Token daha önce çözüldü, doğrulanmış User ID'yi dön.
				return cachedUserId;
			}

			// 3. CACHE MISS: Şimdi pahalı token çözme ve doğrulama işlemini yap.
			// Bu metod şimdi yalnızca Cache Miss olduğunda çalışır!
			var userId = GetUserIdFromToken(context);

			// 4. Cache'e kaydet (5 dakika cache)
			var cacheOptions = new MemoryCacheEntryOptions
			{
				AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5),
				SlidingExpiration = TimeSpan.FromMinutes(2)
			};

			cache.Set(cacheKey, userId, cacheOptions);
			return userId;
		}

		/// <summary>
		/// HttpContext'ten kullanıcı ID'sini alır (JWT claims'den)
		/// </summary>
		public static string? GetUserIdFromContext(HttpContext context)
        {
            // JWT token'dan user ID'yi al
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }

        /// <summary>
        /// Kullanıcının rollerini alır
        /// </summary>
        public static List<string> GetUserRoles(HttpContext context)
        {
            return context.User.FindAll(System.Security.Claims.ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }

        /// <summary>
        /// Kullanıcının belirli bir role sahip olup olmadığını kontrol eder
        /// </summary>
        public static bool HasRole(HttpContext context, string role)
        {
            return GetUserRoles(context).Contains(role);
        }

        /// <summary>
        /// Kullanıcının belirli rollerden birine sahip olup olmadığını kontrol eder
        /// </summary>
        public static bool HasAnyRole(HttpContext context, params string[] roles)
        {
            var userRoles = GetUserRoles(context);
            return roles.Any(role => userRoles.Contains(role));
        }
    }
}
