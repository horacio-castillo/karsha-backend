using Microsoft.AspNetCore.DataProtection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Threading.RateLimiting;
using TarjetasPro.Api.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Auth;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(o => o.Filters.Add<TarjetasPro.Api.Security.AuditFilter>())
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddOpenApi();
// Los secretos de Google se guardan cifrados con estas llaves. En producción, apunta DataProtection:KeysPath a una carpeta que persista (si no, al redesplegar habría que volver a vincular Drive).
var dpb = builder.Services.AddDataProtection().SetApplicationName("KARSHA");
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath) dpb.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();
if (jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key debe tener al menos 32 caracteres (usa user-secrets).");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false;
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = System.Security.Claims.ClaimTypes.Role,
            NameClaimType = "name",
        };
    });
builder.Services.AddAuthorization(o => o.AddPermissionPolicies());

var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddSingleton<TarjetasPro.Api.Presence.PresenceTracker>();
builder.Services.AddCors(o => o.AddDefaultPolicy(p =>
    p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("auth", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(10) }));
    // Login / confirmación / login social: frena fuerza bruta (por IP).
    o.AddPolicy("login", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 8, Window = TimeSpan.FromMinutes(5) }));
    o.AddPolicy("public", ctx => RateLimitPartition.GetFixedWindowLimiter(
        ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1) }));
});

if (!builder.Environment.IsDevelopment() &&
    string.Equals(builder.Configuration["Payments:Provider"], "Simulated", StringComparison.OrdinalIgnoreCase))
    throw new InvalidOperationException("El proveedor de pagos 'Simulated' solo se permite en Development.");

var app = builder.Build();
app.UseMiddleware<TarjetasPro.Api.Security.ErrorCaptureMiddleware>();

await DbSeeder.SeedAsync(app.Services);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles(new StaticFileOptions
{
    // Las fotos y gráficos se pueden leer desde el editor (quitar fondo en el navegador).
    OnPrepareResponse = ctx => ctx.Context.Response.Headers["Access-Control-Allow-Origin"] = "*",
});
app.UseCors();
// Imágenes guardadas en Drive: se entregan desde aquí (el archivo no es público en Drive). Las locales ya las sirvió UseStaticFiles.
app.MapGet("/uploads/drive-{id}.{ext}", async (string id, string ext, TarjetasPro.Infrastructure.Storage.DriveLinkService drive, HttpContext ctx, CancellationToken ct) =>
{
    if (id.Length is < 10 or > 100 || !id.All(c => char.IsLetterOrDigit(c) || c is '-' or '_') || ext is not ("jpg" or "png" or "webp"))
        return Results.NotFound();
    var file = await drive.OpenAsync(id, ct);
    if (file is null) return Results.NotFound();
    ctx.Response.Headers["Access-Control-Allow-Origin"] = "*";
    ctx.Response.Headers.CacheControl = "public, max-age=604800, immutable";
    return Results.File(file.Value.Stream, file.Value.ContentType);
});
// Detrás del proxy de Azure, toma la IP real del cliente: sin esto todas las personas comparten el límite de intentos.
// Si la plataforma ya lo hace (ASPNETCORE_FORWARDEDHEADERS_ENABLED) no se repite: la segunda vuelta leería un dato que escribe el cliente.
if (!string.Equals(app.Configuration["FORWARDEDHEADERS_ENABLED"], "true", StringComparison.OrdinalIgnoreCase))
{
    var forwarded = new ForwardedHeadersOptions
    {
        ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor
                         | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto,
        ForwardLimit = 1, // solo el último dato, el que agrega el proxy
    };
    // Por defecto solo se confía en la propia máquina («{ }» no vacía la lista). A la API solo se llega por el proxy.
    forwarded.KnownIPNetworks.Clear();
    forwarded.KnownProxies.Clear();
    app.UseForwardedHeaders(forwarded);
}
app.UseRateLimiter();
app.UseAuthentication();
// «Ver como cliente» es solo lectura: con esa sesión se rechaza todo lo que cambie datos (salvo vistas previas y cotizaciones).
app.Use(async (ctx, next) =>
{
    var m = ctx.Request.Method;
    if (ctx.User.HasClaim(c => c.Type == TarjetasPro.Domain.Security.ViewAs.AdminClaim)
        && !HttpMethods.IsGet(m) && !HttpMethods.IsHead(m) && !HttpMethods.IsOptions(m)
        && !TarjetasPro.Domain.Security.ViewAs.ReadOnlyPosts.Any(p => ctx.Request.Path.Equals(p, StringComparison.OrdinalIgnoreCase)))
    {
        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsJsonAsync(new { errors = new[] { "Estás viendo como cliente: es solo lectura, no se puede cambiar nada." } });
        return;
    }
    await next();
});
app.UseAuthorization();
app.MapControllers();

app.Run();
