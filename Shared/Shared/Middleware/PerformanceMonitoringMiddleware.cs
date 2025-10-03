using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Shared.Middleware
{
    /// <summary>
    /// Performance monitoring ve metrics toplama middleware'i
    /// </summary>
    public class PerformanceMonitoringMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

        public PerformanceMonitoringMiddleware(RequestDelegate next, ILogger<PerformanceMonitoringMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var stopwatch = Stopwatch.StartNew();
            var requestId = context.TraceIdentifier;

            // Memory kullanımını başlangıçta ölç
            var initialMemory = GC.GetTotalMemory(false);

            try
            {
                await _next(context);
            }
            finally
            {
                stopwatch.Stop();
                var finalMemory = GC.GetTotalMemory(false);
                var memoryUsed = finalMemory - initialMemory;

                // Performance metriklerini logla
                LogPerformanceMetrics(context, stopwatch.ElapsedMilliseconds, memoryUsed, requestId);

                // Yavaş istekleri uyar
                if (stopwatch.ElapsedMilliseconds > 1000) // 1 saniyeden uzun
                {
                    _logger.LogWarning("🐌 [SLOW REQUEST] RequestId: {RequestId}, Path: {Path}, Duration: {Duration}ms",
                        requestId, context.Request.Path, stopwatch.ElapsedMilliseconds);
                }
            }
        }

        private void LogPerformanceMetrics(HttpContext context, long elapsedMs, long memoryUsed, string requestId)
        {
            var metrics = new
            {
                RequestId = requestId,
                Timestamp = DateTime.UtcNow,
                Method = context.Request.Method,
                Path = context.Request.Path.Value,
                StatusCode = context.Response.StatusCode,
                DurationMs = elapsedMs,
                MemoryUsedBytes = memoryUsed,
                MemoryUsedKB = Math.Round(memoryUsed / 1024.0, 2),
                UserId = GetUserIdFromContext(context)
            };

            var logLevel = elapsedMs > 500 ? LogLevel.Warning : LogLevel.Information;
            _logger.Log(logLevel, "⚡ [PERFORMANCE] {@Metrics}", metrics);
        }

        private string? GetUserIdFromContext(HttpContext context)
        {
            var userIdClaim = context.User.FindFirst("sub") ?? 
                             context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return userIdClaim?.Value;
        }
    }
}
