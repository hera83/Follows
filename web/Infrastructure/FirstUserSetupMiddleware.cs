using Microsoft.EntityFrameworkCore;
using web.Data;

namespace web.Infrastructure
{
    /// <summary>
    /// Middleware to redirect to first user setup if no users exist
    /// </summary>
    public class FirstUserSetupMiddleware
    {
        private readonly RequestDelegate _next;

        public FirstUserSetupMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ApplicationDbContext dbContext)
        {
            var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

            // Allow static files and assets
            if (path.StartsWith("/lib/") ||
                path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/images/") ||
                path.StartsWith("/favicon.ico"))
            {
                await _next(context);
                return;
            }

            // Allow setup endpoints
            if (path.StartsWith("/setup/"))
            {
                await _next(context);
                return;
            }

            // Check if any users exist
            var hasUsers = await dbContext.Users.AnyAsync();

            if (!hasUsers)
            {
                // Redirect to first user setup
                if (!path.Equals("/setup/firstuser"))
                {
                    context.Response.Redirect("/Setup/FirstUser");
                    return;
                }
            }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to add FirstUserSetupMiddleware to the pipeline
    /// </summary>
    public static class FirstUserSetupMiddlewareExtensions
    {
        public static IApplicationBuilder UseFirstUserSetup(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<FirstUserSetupMiddleware>();
        }
    }
}
