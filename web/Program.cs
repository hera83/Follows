using Serilog;
using Serilog.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using web.Data;
using web.Data.Entities;
using web.Constants;
using web.Infrastructure;
using web.Repositories.Documents;
using web.Repositories.Documents.Interfaces;
using web.Repositories.Feed;
using web.Repositories.Feed.Interfaces;
using web.Repositories.Logs;
using web.Repositories.Logs.Interfaces;
using web.Repositories.UserProfile;
using web.Repositories.UserProfile.Interfaces;
using web.BgSerives;
using web.Services.AiGateway;
using web.Services.AiGateway.Interfaces;
using web.Services.Mail;
using web.Services.Mail.Interfaces;
using web.Services.Ollama;
using web.Services.Ollama.Interfaces;
using web.Services.Sms;
using web.Services.Sms.Interfaces;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var databasePath = builder.Configuration["AppSettings:DatabasePath"] ?? "App_dbs";
    var logDatabasePath = Path.Combine(builder.Environment.ContentRootPath, databasePath, "logs.db");
    Directory.CreateDirectory(Path.GetDirectoryName(logDatabasePath)!);

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.SQLite(
            sqliteDbPath: logDatabasePath,
            restrictedToMinimumLevel: LogEventLevel.Information,
            storeTimestampInUtc: true,
            rollOver: false)
        .CreateLogger();

    Log.Information("Starting web application");

    // Add Serilog
    builder.Host.UseSerilog();

    // Add Database Context
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Add Identity
    builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password settings
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;

        // Lockout settings
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User settings
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    // Configure cookie paths
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
    });

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("DeveloperOnly", policy =>
            policy.RequireRole(AppRoles.Developer));

        options.AddPolicy("AdminOrDeveloper", policy =>
            policy.RequireRole(AppRoles.Developer, AppRoles.Administrator));
    });

    // Add services to the container.
    builder.Services.AddControllersWithViews();

    // Custom services
    builder.Services.AddScoped<ILogReaderService, LogReaderService>();
    builder.Services.AddHttpClient("Ollama");
    builder.Services.AddSingleton<OllamaHttpClientFactory>();
    builder.Services.AddScoped<IOllamaConfigurationProvider, OllamaConfigurationProvider>();
    builder.Services.AddScoped<IOllamaService, OllamaService>();
    builder.Services.AddHttpClient("AiGateway");
    builder.Services.AddSingleton<AiGatewayHttpClientFactory>();
    builder.Services.AddScoped<IAiGatewayConfigurationProvider, AiGatewayConfigurationProvider>();
    builder.Services.AddScoped<IAiGatewayService, AiGatewayService>();
    var smsBaseUrl = builder.Configuration["Sms:BaseUrl"];
    builder.Services.AddHttpClient<ISmsService, SmsService>(client =>
    {
        // Falder tilbage til en dummy-adresse ligesom Ollama/AiGateway, så DI-opløsning af
        // ISmsService (som SettingsController altid injicerer) ikke fejler når SMS-gatewayen
        // ikke er konfigureret; faktiske kald fejler så blot med HttpRequestException.
        var baseUrl = string.IsNullOrWhiteSpace(smsBaseUrl) ? "http://localhost/" : smsBaseUrl;
        client.BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    });
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IFeedService, FeedService>();
    builder.Services.AddScoped<IDocumentsService, DocumentsService>();
    builder.Services.AddScoped<IMailService, MailService>();

    if (!string.IsNullOrWhiteSpace(builder.Configuration["Sms:ApiKey"]) && !string.IsNullOrWhiteSpace(smsBaseUrl))
    {
        builder.Services.AddHostedService<SmsWorker>();
    }
    else
    {
        Log.Warning("Sms:BaseUrl eller Sms:ApiKey er ikke konfigureret; SmsWorker kører ikke.");
    }

    var app = builder.Build();

    // Initialize database and seed data
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        await web.Data.Seed.DatabaseInitializer.InitializeAsync(services, builder.Configuration, app.Environment);
    }

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    // Trust X-Forwarded-* headers from the reverse proxy (Nginx/Traefik/Caddy)
    // that terminates TLS in front of the container, so UseHttpsRedirection,
    // secure cookies etc. see the original scheme/client IP correctly. Known
    // networks/proxies are cleared because the proxy's address isn't fixed in
    // a container/orchestrator setup.
    var forwardedHeadersOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedHeadersOptions);

    app.UseHttpsRedirection();
    app.UseRouting();

    // First user setup redirect
    app.UseFirstUserSetup();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Feed}/{action=Index}/{id?}")
        .WithStaticAssets();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
