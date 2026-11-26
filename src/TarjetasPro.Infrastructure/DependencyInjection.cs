using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TarjetasPro.Application.Auth;
using TarjetasPro.Infrastructure.Auth;
using TarjetasPro.Infrastructure.Identity;
using TarjetasPro.Infrastructure.Payments;
using TarjetasPro.Infrastructure.Storage;

namespace TarjetasPro.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o =>
            o.UseSqlServer(config.GetConnectionString("Default")));

        services.AddIdentityCore<ApplicationUser>(o =>
            {
                o.Password.RequiredLength = 8;
                o.Password.RequireNonAlphanumeric = false;
                o.User.RequireUniqueEmail = true;
                o.Lockout.MaxFailedAccessAttempts = 5;
                o.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders()
            .AddErrorDescriber<TarjetasPro.Infrastructure.Auth.SpanishIdentityErrors>();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<SeedAdminOptions>(config.GetSection(SeedAdminOptions.Section));
        services.Configure<ExternalAuthOptions>(config.GetSection(ExternalAuthOptions.Section));
        services.AddHttpClient<ExternalTokenVerifier>(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.Section));
        // En desarrollo nunca se manda correo real (aunque haya SMTP en user-secrets): solo se loguea.
        var envName = config["ASPNETCORE_ENVIRONMENT"] ?? config["DOTNET_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        if (string.Equals(envName, "Development", StringComparison.OrdinalIgnoreCase))
            services.PostConfigure<EmailOptions>(o => o.LogOnly = true);
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<PaymentOptions>(config.GetSection(PaymentOptions.Section));
        if (string.Equals(config[$"{PaymentOptions.Section}:Provider"], "Simulated", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
        else
            services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<NotificationService>();
        services.AddScoped<PaymentService>();
        services.Configure<DriveOptions>(config.GetSection(DriveOptions.Section));
        services.AddScoped<DriveLinkService>();
        services.AddScoped<DriveStorageService>();
        services.AddSingleton<TarjetasPro.Infrastructure.Logging.SystemLogger>();
        services.AddHostedService<CardExpiryService>();
        services.AddHttpClient(nameof(TarjetasPro.Infrastructure.Analytics.GeoIpService), c => c.Timeout = TimeSpan.FromSeconds(5));
        services.AddSingleton<TarjetasPro.Infrastructure.Analytics.GeoIpService>();
        services.AddHostedService(sp => sp.GetRequiredService<TarjetasPro.Infrastructure.Analytics.GeoIpService>());
        return services;
    }
}
