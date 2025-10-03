using Microsoft.AspNetCore.Builder;
using Shared.Middleware;

namespace Shared.Extensions
{
    /// <summary>
    /// Merkezi middleware konfigürasyonu için extension metodları
    /// </summary>
    public static class MiddlewareExtensions
    {
        /// <summary>
        /// Tüm merkezi middleware'leri ekler
        /// </summary>
        public static IApplicationBuilder UseCentralizedMiddleware(this IApplicationBuilder app)
        {
            // Middleware sırası önemli!
            
            // 1. Global Exception Handling (en dış katman)
            app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
            
            // 2. Performance Monitoring
            app.UseMiddleware<PerformanceMonitoringMiddleware>();
            
            // 3. Request/Response Logging
            app.UseMiddleware<RequestResponseLoggingMiddleware>();

            return app;
        }

        /// <summary>
        /// Sadece Request/Response Logging middleware'ini ekler
        /// </summary>
        public static IApplicationBuilder UseRequestResponseLogging(this IApplicationBuilder app)
        {
            return app.UseMiddleware<RequestResponseLoggingMiddleware>();
        }

        /// <summary>
        /// Sadece Global Exception Handling middleware'ini ekler
        /// </summary>
        public static IApplicationBuilder UseGlobalExceptionHandling(this IApplicationBuilder app)
        {
            return app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        }

        /// <summary>
        /// Sadece Performance Monitoring middleware'ini ekler
        /// </summary>
        public static IApplicationBuilder UsePerformanceMonitoring(this IApplicationBuilder app)
        {
            return app.UseMiddleware<PerformanceMonitoringMiddleware>();
        }
    }
}
