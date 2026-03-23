using CineBook.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using System.Text;
using CineBook.API.Hubs;
using QuestPDF.Infrastructure;
using CineBook.Application.DTOs.Requests;

try
{
    QuestPDF.Settings.License = LicenseType.Community;

    var builder = WebApplication.CreateBuilder(args);

    // Add serilogsettings.json to configuration
    builder.Configuration
        .AddJsonFile("serilogsettings.json", optional: true);

    builder.Host.UseSerilog((context, config) =>
        config.ReadFrom.Configuration(context.Configuration));

    // Infrastructure (DB + Identity)
    builder.Services.AddInfrastructure(builder.Configuration);

    builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("StripeSettings"));

    // Controllers + Views
    builder.Services.AddControllersWithViews();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    // JWT - Validate configuration before use
    var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
    var jwtIssuer = builder.Configuration["JwtSettings:Issuer"];
    var jwtAudience = builder.Configuration["JwtSettings:Audience"];

    if (string.IsNullOrWhiteSpace(jwtKey))
    {
        throw new InvalidOperationException("JwtSettings:SecretKey is not configured in appsettings.json");
    }
    if (string.IsNullOrWhiteSpace(jwtIssuer))
    {
        throw new InvalidOperationException("JwtSettings:Issuer is not configured in appsettings.json");
    }
    if (string.IsNullOrWhiteSpace(jwtAudience))
    {
        throw new InvalidOperationException("JwtSettings:Audience is not configured in appsettings.json");
    }

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

    // SignalR
    builder.Services.AddSignalR();

    Log.Information("Starting CineBook API...");
    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }
    else
    {
        app.UseExceptionHandler("/Home/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapHub<SeatHub>("/hubs/seats");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    Log.Information("CineBook API started successfully");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine("FATAL ERROR OCCURRED:");
    Console.WriteLine("═══════════════════════════════════════════════════════════");
    Console.WriteLine($"Message: {ex.Message}");
    Console.WriteLine($"\nStack Trace:\n{ex.StackTrace}");

    if (ex.InnerException != null)
    {
        Console.WriteLine($"\n\nINNER EXCEPTION:");
        Console.WriteLine($"Message: {ex.InnerException.Message}");
        Console.WriteLine($"Stack Trace:\n{ex.InnerException.StackTrace}");
    }

    Console.WriteLine("\n═══════════════════════════════════════════════════════════");
    Console.WriteLine("Press any key to exit...");
    Console.ReadKey();

    Log.Fatal(ex, "CineBook API terminated unexpectedly.");
    Environment.Exit(1);
}
finally
{
    Log.CloseAndFlush();
}