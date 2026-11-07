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
            .AddDefaultTokenProviders();

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.Section));
        services.Configure<SeedAdminOptions>(config.GetSection(SeedAdminOptions.Section));
        services.Configure<ExternalAuthOptions>(config.GetSection(ExternalAuthOptions.Section));
        services.AddHttpClient<ExternalTokenVerifier>(c => c.Timeout = TimeSpan.FromSeconds(10));
        services.Configure<EmailOptions>(config.GetSection(EmailOptions.Section));
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<PaymentOptions>(config.GetSection(PaymentOptions.Section));
        if (string.Equals(config[$"{PaymentOptions.Section}:Provider"], "Simulated", StringComparison.OrdinalIgnoreCase))
            services.AddScoped<IPaymentProvider, SimulatedPaymentProvider>();
        else
            services.AddScoped<IPaymentProvider, StripePaymentProvider>();
        services.AddScoped<PaymentService>();
        services.Configure<DriveOptions>(config.GetSection(DriveOptions.Section));
        services.AddScoped<DriveStorageService>();
        services.AddHostedService<CardExpiryService>();
        return services;
    }
}
