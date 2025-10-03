using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace Shared.Middleware
{
    /// <summary>
    /// Request ve Response bilgilerini loglayan middleware
    /// </summary>
    public class RequestResponseLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestResponseLoggingMiddleware> _logger;

        public RequestResponseLoggingMiddleware(RequestDelegate next, ILogger<RequestResponseLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = Guid.NewGuid().ToString("N")[..8];

            // Request bilgilerini logla
            await LogRequestAsync(context, requestId);

            // Response'u yakalamak için stream'i wrap et
            var originalBodyStream = context.Response.Body;
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();

                // Response bilgilerini logla
                await LogResponseAsync(context, requestId, stopwatch.ElapsedMilliseconds);

                // Response'u orijinal stream'e kopyala
                await responseBody.CopyToAsync(originalBodyStream);
            }
        }

        private async Task LogRequestAsync(HttpContext context, string requestId)
        {
            var request = context.Request;
            var userId = GetUserIdFromContext(context);

            var logData = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = request.Method,
                Path = request.Path.Value,
                QueryString = request.QueryString.Value,
                UserId = userId,
                UserAgent = request.Headers.UserAgent.ToString(),
                ContentType = request.ContentType,
                ContentLength = request.ContentLength,
                RemoteIpAddress = context.Connection.RemoteIpAddress?.ToString(),
                Headers = GetSafeHeaders(request.Headers)
            };

            _logger.LogInformation("🚀 [REQUEST] {@LogData}", logData);

            // Request body'yi logla (sadece POST, PUT, PATCH için)
            if (ShouldLogRequestBody(request.Method))
            {
                var body = await ReadRequestBodyAsync(request);
                if (!string.IsNullOrEmpty(body))
                {
                    _logger.LogInformation("📝 [REQUEST BODY] RequestId: {RequestId}, Body: {Body}", requestId, body);
                }
            }
        }

        private async Task LogResponseAsync(HttpContext context, string requestId, long elapsedMs)
        {
            var response = context.Response;
            var userId = GetUserIdFromContext(context);

            // Response body'yi oku
            var responseBody = await ReadResponseBodyAsync(response);

            var logData = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                StatusCode = response.StatusCode,
                ContentType = response.ContentType,
                ContentLength = response.ContentLength,
                ElapsedMs = elapsedMs,
                UserId = userId,
                Headers = GetSafeHeaders(response.Headers)
            };

            var logLevel = response.StatusCode >= 400 ? LogLevel.Warning : LogLevel.Information;
            var emoji = response.StatusCode >= 400 ? "❌" : "✅";

            _logger.Log(logLevel, "{Emoji} [RESPONSE] {@LogData}", emoji, logData);

            // Response body'yi logla (sadece hata durumlarında veya küçük response'lar için)
            if (ShouldLogResponseBody(response.StatusCode, responseBody))
            {
                _logger.LogInformation("📤 [RESPONSE BODY] RequestId: {RequestId}, Body: {Body}", requestId, responseBody);
            }
        }

        private string? GetUserIdFromContext(HttpContext context)
        {
            // JWT token'dan user ID'yi al
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }

        private Dictionary<string, string> GetSafeHeaders(IHeaderDictionary headers)
        {
            var safeHeaders = new Dictionary<string, string>();
            var sensitiveHeaders = new[] { "authorization", "cookie", "x-api-key" };

            foreach (var header in headers)
            {
                if (sensitiveHeaders.Contains(header.Key.ToLower()))
                {
                    safeHeaders[header.Key] = "***REDACTED***";
                }
                else
                {
                    safeHeaders[header.Key] = string.Join(", ", header.Value);
                }
            }

            return safeHeaders;
        }

        private async Task<string> ReadRequestBodyAsync(HttpRequest request)
        {
            try
            {
                request.EnableBuffering();
                request.Body.Position = 0;
                using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                request.Body.Position = 0;
                return body;
            }
            catch
            {
                return string.Empty;
            }
        }

        private async Task<string> ReadResponseBodyAsync(HttpResponse response)
        {
            try
            {
                response.Body.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                response.Body.Seek(0, SeekOrigin.Begin);
                return body;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static bool ShouldLogRequestBody(string method)
        {
            return method is "POST" or "PUT" or "PATCH";
        }

        private static bool ShouldLogResponseBody(int statusCode, string responseBody)
        {
            // Sadece hata durumlarında veya küçük response'larda logla
            return statusCode >= 400 || (responseBody.Length < 1000 && !string.IsNullOrEmpty(responseBody));
        }
    }
}
