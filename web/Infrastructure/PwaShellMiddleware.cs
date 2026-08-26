using Microsoft.AspNetCore.Http;

namespace web.Infrastructure
{
    /// <summary>
    /// Recognizes requests under the "/app" URL space — the installable PWA entry point scoped to
    /// Feeds/Documents/Min Profil (Indstillinger is never reachable there) — and rewrites the request
    /// path to the plain, already-existing route shape before routing runs.
    ///
    /// Deliberately does NOT register a second MapControllerRoute for "/app/{controller}/...": having
    /// two conventional routes that can both satisfy the same {controller, action} route values would
    /// make ASP.NET Core's outbound link generation ambiguous for every asp-controller/asp-action tag
    /// helper and RedirectToAction call in the entire app (including inside _Layout.cshtml's own
    /// sidebar), since both routes could produce a valid URL. Rewriting the path here instead means
    /// routing/MVC only ever sees the one route that already exists — zero risk to any existing link.
    ///
    /// Only Feed/Documents/Profile are whitelisted; anything else under /app/* is left un-rewritten and
    /// falls through to normal routing, which 404s (there is no "AppController").
    /// </summary>
    public class PwaShellMiddleware
    {
        private static readonly string[] AllowedControllers = { "Feed", "Documents", "Profile" };

        private readonly RequestDelegate _next;

        public PwaShellMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public Task InvokeAsync(HttpContext context)
        {
            if (!context.Request.Path.StartsWithSegments("/app", out var remaining))
            {
                return _next(context);
            }

            // Bare "/app" or "/app/" — the PWA manifest's start_url — defaults to the Feeds tab.
            if (remaining == PathString.Empty || remaining.Value == "/")
            {
                Rewrite(context, "/Feed");
                return _next(context);
            }

            var firstSegment = remaining.Value!.TrimStart('/').Split('/')[0];
            var isAllowed = AllowedControllers.Any(c => string.Equals(c, firstSegment, StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                // Not one of the whitelisted controllers (e.g. someone typed /app/Settings) — leave
                // unrewritten and un-flagged; normal routing 404s since no "AppController" exists.
                return _next(context);
            }

            Rewrite(context, remaining.Value!);
            return _next(context);
        }

        private static void Rewrite(HttpContext context, string newPath)
        {
            // Kept for the login-challenge ReturnUrl (see Program.cs's OnRedirectToLogin) — by the time
            // the auth handler builds that redirect, context.Request.Path is already the rewritten one.
            context.Items["PwaOriginalPath"] = context.Request.Path.Value;
            context.Items["PwaShell"] = true;
            context.Request.Path = newPath;
        }
    }

    /// <summary>
    /// Extension method to add PwaShellMiddleware to the pipeline.
    /// </summary>
    public static class PwaShellMiddlewareExtensions
    {
        public static IApplicationBuilder UsePwaShell(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PwaShellMiddleware>();
        }
    }
}
