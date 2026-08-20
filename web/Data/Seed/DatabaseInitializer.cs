using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using web.Constants;
using web.Data.Entities;

namespace web.Data.Seed
{
    /// <summary>
    /// Database initializer - ensures folders, runs migrations, and seeds default data
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initialize database, folders, roles, and default settings
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider, IConfiguration configuration, IWebHostEnvironment environment)
        {
            using var scope = serviceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                // Ensure folders exist
                EnsureFoldersExist(configuration, environment, logger);

                // Get database context
                var context = services.GetRequiredService<ApplicationDbContext>();

                // Run migrations
                logger.LogInformation("Running database migrations...");
                await context.Database.MigrateAsync();
                logger.LogInformation("Database migrations completed");

                // Seed roles
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
                await SeedRolesAsync(roleManager, logger);

                // Seed default settings
                await SeedDefaultSettingsAsync(context, logger);

                // Seed default themes
                await SeedDefaultThemesAsync(context, logger);

                logger.LogInformation("Database initialization completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while initializing the database");
                throw;
            }
        }

        /// <summary>
        /// Ensure App_dbs and App_files folders exist
        /// </summary>
        private static void EnsureFoldersExist(IConfiguration configuration, IWebHostEnvironment environment, ILogger logger)
        {
            var dbPath = configuration["AppSettings:DatabasePath"] ?? "App_dbs";
            var filesPath = configuration["AppSettings:FilesPath"] ?? "App_files";

            var dbFullPath = Path.Combine(environment.ContentRootPath, dbPath);
            var filesFullPath = Path.Combine(environment.ContentRootPath, filesPath);

            if (!Directory.Exists(dbFullPath))
            {
                Directory.CreateDirectory(dbFullPath);
                logger.LogInformation("Created directory: {DbPath}", dbFullPath);
            }

            if (!Directory.Exists(filesFullPath))
            {
                Directory.CreateDirectory(filesFullPath);
                logger.LogInformation("Created directory: {FilesPath}", filesFullPath);
            }

            // Ensure standard App_files subcategory folders exist
            foreach (var category in new[] { FileCategories.Avatars, FileCategories.Uploads, FileCategories.Exports, FileCategories.Temp })
            {
                var categoryPath = Path.Combine(filesFullPath, category);
                if (!Directory.Exists(categoryPath))
                {
                    Directory.CreateDirectory(categoryPath);
                    logger.LogInformation("Created directory: {CategoryPath}", categoryPath);
                }
            }
        }

        /// <summary>
        /// Seed application roles
        /// </summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager, ILogger logger)
        {
            foreach (var roleName in AppRoles.AllRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                    if (result.Succeeded)
                    {
                        logger.LogInformation("Created role: {RoleName}", roleName);
                    }
                    else
                    {
                        logger.LogError("Failed to create role {RoleName}: {Errors}",
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        /// <summary>
        /// Seed default application settings
        /// </summary>
        private static async Task SeedDefaultSettingsAsync(ApplicationDbContext context, ILogger logger)
        {
            if (!await context.AppSettings.AnyAsync())
            {
                var defaultSettings = new List<AppSetting>
                {
                    new AppSetting
                    {
                        Key = AppSettingKeys.AllowPublicRegistration,
                        Value = "false",
                        Description = "Controls whether public user registration is allowed",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new AppSetting
                    {
                        Key = AppSettingKeys.ActiveThemeMode,
                        Value = ThemeMode.System,
                        Description = "Active theme mode: Light, Dark, or System",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new AppSetting
                    {
                        Key = AppSettingKeys.ActiveThemeName,
                        Value = "Orange",
                        Description = "Name of the active color theme, used for both Light and Dark mode",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new AppSetting
                    {
                        Key = AppSettingKeys.SiteName,
                        Value = "Follows",
                        Description = "Application display name",
                        CreatedAtUtc = DateTime.UtcNow
                    },
                    new AppSetting
                    {
                        Key = AppSettingKeys.SiteLogo,
                        Value = "/images/logo.png",
                        Description = "Application logo path",
                        CreatedAtUtc = DateTime.UtcNow
                    }
                };

                await context.AppSettings.AddRangeAsync(defaultSettings);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} default settings", defaultSettings.Count);
            }
        }

        /// <summary>
        /// Rename theme presets from their old names to the current names, so installs
        /// seeded before a rename keep their existing colors under the new name.
        /// </summary>
        private static async Task RenameLegacyThemePresetsAsync(ApplicationDbContext context, ILogger logger)
        {
            var renames = new Dictionary<string, string>
            {
                ["Default"] = "Orange",
                ["Blåtonet"] = "Blå"
            };

            var changed = false;

            foreach (var (oldName, newName) in renames)
            {
                var rows = await context.ThemeSettings.Where(t => t.Name == oldName).ToListAsync();
                if (rows.Count == 0)
                    continue;

                if (await context.ThemeSettings.AnyAsync(t => t.Name == newName))
                    continue;

                foreach (var row in rows)
                    row.Name = newName;

                logger.LogInformation("Renamed theme preset {OldName} to {NewName}", oldName, newName);
                changed = true;
            }

            var activeThemeSetting = await context.AppSettings.FirstOrDefaultAsync(s => s.Key == AppSettingKeys.ActiveThemeName);
            if (activeThemeSetting is not null && renames.TryGetValue(activeThemeSetting.Value, out var mappedName))
            {
                activeThemeSetting.Value = mappedName;
                changed = true;
            }

            if (changed)
                await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed default color theme presets (Light/Dark), adding any preset that is
        /// missing without touching presets that already exist in the database.
        /// </summary>
        private static async Task SeedDefaultThemesAsync(ApplicationDbContext context, ILogger logger)
        {
            await RenameLegacyThemePresetsAsync(context, logger);

            var presets = new List<ThemeSetting>
            {
                // Orange Light Theme
                new ThemeSetting
                {
                    Name = "Orange",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#c94e24",
                    SecondaryColor = "#4b5563",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#1d5db5",
                    BackgroundColor = "#f0f2f5",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#111827",
                    TextMutedColor = "#4b5563",
                    BorderColor = "#d1d5db",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Orange Dark Theme
                new ThemeSetting
                {
                    Name = "Orange",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#e85d2e",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#388bfd",
                    BackgroundColor = "#0d1117",
                    SurfaceColor = "#161b22",
                    SidebarBackgroundColor = "#161b22",
                    TextColor = "#e6edf3",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#30363d",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Blå Light Theme
                new ThemeSetting
                {
                    Name = "Blå",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#2563eb",
                    SecondaryColor = "#475569",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#0891b2",
                    BackgroundColor = "#eff4fb",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#0f172a",
                    TextMutedColor = "#475569",
                    BorderColor = "#cbd5e1",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Blå Dark Theme
                new ThemeSetting
                {
                    Name = "Blå",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#3b82f6",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#22d3ee",
                    BackgroundColor = "#0b1220",
                    SurfaceColor = "#111a2e",
                    SidebarBackgroundColor = "#111a2e",
                    TextColor = "#e6edf3",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#243044",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Grøn Light Theme
                new ThemeSetting
                {
                    Name = "Grøn",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#15803d",
                    SecondaryColor = "#4b5563",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#0e7490",
                    BackgroundColor = "#eef7f0",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#0f291a",
                    TextMutedColor = "#4b5563",
                    BorderColor = "#cfe3d4",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Grøn Dark Theme
                new ThemeSetting
                {
                    Name = "Grøn",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#22c55e",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#22d3ee",
                    BackgroundColor = "#0b1410",
                    SurfaceColor = "#11201a",
                    SidebarBackgroundColor = "#11201a",
                    TextColor = "#e6f3ea",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#1f3329",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Lilla Light Theme
                new ThemeSetting
                {
                    Name = "Lilla",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#7c3aed",
                    SecondaryColor = "#4b5563",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#0891b2",
                    BackgroundColor = "#f4f0fb",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#211238",
                    TextMutedColor = "#4b5563",
                    BorderColor = "#ddd3f3",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Lilla Dark Theme
                new ThemeSetting
                {
                    Name = "Lilla",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#a78bfa",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#38bdf8",
                    BackgroundColor = "#15101f",
                    SurfaceColor = "#1c1730",
                    SidebarBackgroundColor = "#1c1730",
                    TextColor = "#ece8f9",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#2e2650",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Rød Light Theme
                new ThemeSetting
                {
                    Name = "Rød",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#dc2626",
                    SecondaryColor = "#4b5563",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#1d5db5",
                    BackgroundColor = "#fcecec",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#2a0e0e",
                    TextMutedColor = "#4b5563",
                    BorderColor = "#f0cccb",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Rød Dark Theme
                new ThemeSetting
                {
                    Name = "Rød",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#f87171",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#388bfd",
                    BackgroundColor = "#1a0e0e",
                    SurfaceColor = "#241414",
                    SidebarBackgroundColor = "#241414",
                    TextColor = "#f8e6e6",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#3d1f1f",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Teal Light Theme
                new ThemeSetting
                {
                    Name = "Teal",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#0d9488",
                    SecondaryColor = "#4b5563",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#0891b2",
                    BackgroundColor = "#eafaf7",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#0d2b27",
                    TextMutedColor = "#4b5563",
                    BorderColor = "#c9ebe5",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Teal Dark Theme
                new ThemeSetting
                {
                    Name = "Teal",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#2dd4bf",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#22d3ee",
                    BackgroundColor = "#081513",
                    SurfaceColor = "#0f201d",
                    SidebarBackgroundColor = "#0f201d",
                    TextColor = "#e3f7f3",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#1c3a35",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Slate Light Theme
                new ThemeSetting
                {
                    Name = "Slate",
                    ThemeMode = ThemeMode.Light,
                    PrimaryColor = "#475569",
                    SecondaryColor = "#64748b",
                    SuccessColor = "#16803c",
                    DangerColor = "#c8160c",
                    WarningColor = "#a16207",
                    InfoColor = "#1d5db5",
                    BackgroundColor = "#f1f4f7",
                    SurfaceColor = "#ffffff",
                    SidebarBackgroundColor = "#ffffff",
                    TextColor = "#1e293b",
                    TextMutedColor = "#64748b",
                    BorderColor = "#d8dee5",
                    CreatedAtUtc = DateTime.UtcNow
                },
                // Slate Dark Theme
                new ThemeSetting
                {
                    Name = "Slate",
                    ThemeMode = ThemeMode.Dark,
                    PrimaryColor = "#94a3b8",
                    SecondaryColor = "#7d8590",
                    SuccessColor = "#2ea043",
                    DangerColor = "#f85149",
                    WarningColor = "#d29922",
                    InfoColor = "#388bfd",
                    BackgroundColor = "#0f1419",
                    SurfaceColor = "#161c22",
                    SidebarBackgroundColor = "#161c22",
                    TextColor = "#e7ebef",
                    TextMutedColor = "#7d8590",
                    BorderColor = "#2a323c",
                    CreatedAtUtc = DateTime.UtcNow
                }
            };

            var existing = await context.ThemeSettings
                .Select(t => new { t.Name, t.ThemeMode })
                .ToListAsync();

            var missingPresets = presets
                .Where(p => !existing.Any(e => e.Name == p.Name && e.ThemeMode == p.ThemeMode))
                .ToList();

            if (missingPresets.Count > 0)
            {
                await context.ThemeSettings.AddRangeAsync(missingPresets);
                await context.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} missing theme presets", missingPresets.Count);
            }
        }
    }
}
