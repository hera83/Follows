using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using web.Data;
using web.Data.Entities;

namespace web.Controllers
{
    public class ThemeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ThemeController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("theme.css")]
        [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Client)]
        public async Task<IActionResult> Css()
        {
            var activeMode = await GetAppSettingAsync(AppSettingKeys.ActiveThemeMode, ThemeMode.System);
            var activeThemeName = await GetAppSettingAsync(AppSettingKeys.ActiveThemeName, "Orange");

            var lightTheme = await _context.ThemeSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == activeThemeName && t.ThemeMode == ThemeMode.Light);

            var darkTheme = await _context.ThemeSettings
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == activeThemeName && t.ThemeMode == ThemeMode.Dark);

            var css = new StringBuilder();

            if (activeMode == ThemeMode.Dark && darkTheme is not null)
            {
                css.AppendLine(BuildVariables(":root", darkTheme));
            }
            else if (activeMode == ThemeMode.Light && lightTheme is not null)
            {
                css.AppendLine(BuildVariables("[data-theme=\"light\"]", lightTheme));
            }
            else
            {
                if (darkTheme is not null)
                {
                    css.AppendLine(BuildVariables(":root", darkTheme));
                }

                if (lightTheme is not null)
                {
                    css.AppendLine(BuildVariables("[data-theme=\"light\"]", lightTheme));
                }
            }

            return Content(css.ToString(), "text/css", Encoding.UTF8);
        }

        private async Task<string> GetAppSettingAsync(string key, string defaultValue)
        {
            var value = await _context.AppSettings
                .AsNoTracking()
                .Where(s => s.Key == key)
                .Select(s => s.Value)
                .FirstOrDefaultAsync();

            return value ?? defaultValue;
        }

        private static string BuildVariables(string selector, ThemeSetting theme)
        {
            var css = new StringBuilder();
            css.AppendLine($"{selector} {{");
            css.AppendLine($"  --clr-brand: {theme.PrimaryColor};");
            css.AppendLine($"  --clr-ok: {theme.SuccessColor};");
            css.AppendLine($"  --clr-alarm: {theme.DangerColor};");
            css.AppendLine($"  --clr-warn: {theme.WarningColor};");
            css.AppendLine($"  --clr-info: {theme.InfoColor};");
            css.AppendLine($"  --clr-bg: {theme.BackgroundColor};");
            css.AppendLine($"  --clr-surface: {theme.SurfaceColor};");
            css.AppendLine($"  --clr-text: {theme.TextColor};");
            css.AppendLine($"  --clr-text-muted: {theme.TextMutedColor};");
            css.AppendLine($"  --clr-border: {theme.BorderColor};");
            css.AppendLine("}");
            return css.ToString();
        }
    }
}
