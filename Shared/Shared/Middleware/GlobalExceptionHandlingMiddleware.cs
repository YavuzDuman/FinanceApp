using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware
{
    /// <summary>
    /// Global hata yakalama ve loglama middleware'i
    /// </summary>
    public class GlobalExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionHandlingMiddleware> _logger;

        public GlobalExceptionHandlingMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var requestId = context.TraceIdentifier;
            var userId = GetUserIdFromContext(context);

            // Hata detaylarını logla
            _logger.LogError(exception, 
                "💥 [EXCEPTION] RequestId: {RequestId}, UserId: {UserId}, Path: {Path}, Method: {Method}",
                requestId, userId, context.Request.Path, context.Request.Method);

            // Response'u hazırla
            context.Response.ContentType = "application/json";
            context.Response.StatusCode = GetStatusCode(exception);

            var errorResponse = new ErrorResponse
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                StatusCode = context.Response.StatusCode,
                Message = GetErrorMessage(exception),
                Path = context.Request.Path.Value,
                Method = context.Request.Method
            };

            // Development ortamında stack trace ekle
            if (IsDevelopment())
            {
                errorResponse.StackTrace = exception.StackTrace;
                errorResponse.Details = exception.ToString();
            }

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            await context.Response.WriteAsync(jsonResponse);
        }

        private string? GetUserIdFromContext(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }

        private static int GetStatusCode(Exception exception)
        {
            return exception switch
            {
                ArgumentException => (int)HttpStatusCode.BadRequest,
                UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
                NotImplementedException => (int)HttpStatusCode.NotImplemented,
                TimeoutException => (int)HttpStatusCode.RequestTimeout,
                _ => (int)HttpStatusCode.InternalServerError
            };
        }

        private static string GetErrorMessage(Exception exception)
        {
            return exception switch
            {
                ArgumentException => "Geçersiz parametre gönderildi.",
                UnauthorizedAccessException => "Bu işlem için yetkiniz bulunmuyor.",
                NotImplementedException => "Bu özellik henüz geliştirilmedi.",
                TimeoutException => "İşlem zaman aşımına uğradı.",
                _ => "Beklenmeyen bir hata oluştu."
            };
        }

        private static bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        }
    }

    public class ErrorResponse
    {
        public string RequestId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Method { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string? Details { get; set; }
    }
}
