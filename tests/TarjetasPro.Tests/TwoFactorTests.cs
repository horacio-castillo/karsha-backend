using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using TarjetasPro.Application.Auth;
using TarjetasPro.Domain.Security;
using TarjetasPro.Infrastructure;
using TarjetasPro.Infrastructure.Auth;
using TarjetasPro.Infrastructure.Identity;

namespace TarjetasPro.Tests;

/// <summary>Verificación en dos pasos (app de autenticación y códigos de respaldo) y la sesión de «ver como cliente».</summary>
public sealed class TwoFactorTests : IDisposable
{
    private const string Password = "Prueba1234";
    private readonly SqliteConnection _conn = new("DataSource=:memory:");
    private readonly ServiceProvider _sp;
    private readonly UserManager<ApplicationUser> _users;
    private readonly AuthService _auth;
    private readonly ApplicationUser _user;

    public TwoFactorTests()
    {
        _conn.Open();
        var s = new ServiceCollection();
        s.AddLogging();
        s.AddDataProtection();
        s.AddDbContext<AppDbContext>(o => o.UseSqlite(_conn));
        s.AddIdentityCore<ApplicationUser>(o => { o.Password.RequireNonAlphanumeric = false; o.Lockout.MaxFailedAccessAttempts = 5; })
            .AddRoles<ApplicationRole>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
        _sp = s.BuildServiceProvider();
        var db = _sp.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        _users = _sp.GetRequiredService<UserManager<ApplicationUser>>();
        _sp.GetRequiredService<RoleManager<ApplicationRole>>().CreateAsync(new ApplicationRole { Name = Roles.Cliente }).GetAwaiter().GetResult();

        _user = new ApplicationUser { UserName = "ana@test.mx", Email = "ana@test.mx", EmailConfirmed = true, FullName = "Ana" };
        _users.CreateAsync(_user, Password).GetAwaiter().GetResult();
        _users.AddToRoleAsync(_user, Roles.Cliente).GetAwaiter().GetResult();

        var jwt = Options.Create(new JwtOptions { Key = new string('k', 48) });
        var ext = Options.Create(new ExternalAuthOptions());
        _auth = new AuthService(_users, _sp.GetRequiredService<RoleManager<ApplicationRole>>(), db, jwt,
            new ExternalTokenVerifier(new HttpClient(), ext), ext, new NoEmail(), new ConfigurationBuilder().Build(), NullLogger<AuthService>.Instance);
    }

    public void Dispose() { _sp.Dispose(); _conn.Dispose(); }

    private async Task<string> TurnOnAsync()
    {
        await _users.ResetAuthenticatorKeyAsync(_user);
        await _users.SetTwoFactorEnabledAsync(_user, true);
        return (await _users.GetAuthenticatorKeyAsync(_user))!;
    }

    [Fact]
    public async Task Sin_2FA_entra_directo()
    {
        var r = await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password));
        Assert.NotNull(r.Data);
        Assert.Null(r.TwoFactorTicket);
    }

    [Fact]
    public async Task Con_2FA_la_contrasena_sola_no_da_sesion()
    {
        await TurnOnAsync();
        var r = await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password));
        Assert.Null(r.Data);
        Assert.NotNull(r.TwoFactorTicket);
    }

    [Fact]
    public async Task Con_el_codigo_de_la_app_entra()
    {
        var key = await TurnOnAsync();
        var ticket = (await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password))).TwoFactorTicket!;
        var r = await _auth.TwoFactorLoginAsync(new TwoFactorLoginRequest(ticket, Totp(key)));
        Assert.NotNull(r.Data);
        Assert.NotEmpty(r.Data!.RefreshToken);
    }

    [Fact]
    public async Task Codigo_incorrecto_o_pase_alterado_no_entran()
    {
        var key = await TurnOnAsync();
        var ticket = (await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password))).TwoFactorTicket!;
        Assert.Null((await _auth.TwoFactorLoginAsync(new TwoFactorLoginRequest(ticket, "000000"))).Data);
        var forged = ticket[..^1] + (ticket[^1] == 'A' ? 'B' : 'A');
        Assert.Null((await _auth.TwoFactorLoginAsync(new TwoFactorLoginRequest(forged, Totp(key)))).Data);
    }

    [Fact]
    public async Task Codigo_de_respaldo_sirve_una_sola_vez_y_sin_guion()
    {
        await TurnOnAsync();
        var code = (await _users.GenerateNewTwoFactorRecoveryCodesAsync(_user, 10))!.First();
        var typed = code.Replace("-", "").ToLowerInvariant();
        var t1 = (await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password))).TwoFactorTicket!;
        Assert.NotNull((await _auth.TwoFactorLoginAsync(new TwoFactorLoginRequest(t1, typed))).Data);
        var t2 = (await _auth.LoginAsync(new LoginRequest("ana@test.mx", Password))).TwoFactorTicket!;
        Assert.Null((await _auth.TwoFactorLoginAsync(new TwoFactorLoginRequest(t2, typed))).Data);
    }

    [Fact]
    public async Task Ver_como_cliente_no_se_renueva_y_lleva_quien_lo_abrio()
    {
        var s = await _auth.ViewAsAsync(_user.Id, Guid.NewGuid().ToString(), "Horacio");
        Assert.NotNull(s);
        Assert.Equal("", s!.RefreshToken);
        Assert.Equal("Horacio", s.ViewedBy);
        var jwt = new JsonWebToken(s.AccessToken);
        Assert.Equal("Horacio", jwt.GetClaim(ViewAs.AdminNameClaim).Value);
        Assert.True(s.ExpiresAt <= DateTime.UtcNow.AddMinutes(31));
    }

    [Fact]
    public void La_llave_se_muestra_en_grupos_y_el_enlace_trae_la_marca()
    {
        Assert.Equal("abcd efgh ij", TwoFactorCodes.Pretty("ABCDEFGHIJ"));
        Assert.StartsWith("otpauth://totp/KARSHA%3Aana%40test.mx?secret=ABC&issuer=KARSHA", TwoFactorCodes.Uri("ana@test.mx", "ABC"));
    }

    /// <summary>Código TOTP de 6 dígitos (RFC 6238) como lo calcula la app del celular.</summary>
    private static string Totp(string base32)
    {
        var key = Base32(base32);
        var step = DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30;
        var msg = BitConverter.GetBytes(step); if (BitConverter.IsLittleEndian) Array.Reverse(msg);
        var h = HMACSHA1.HashData(key, msg);
        var o = h[^1] & 0xf;
        var bin = ((h[o] & 0x7f) << 24) | (h[o + 1] << 16) | (h[o + 2] << 8) | h[o + 3];
        return (bin % 1_000_000).ToString("D6");
    }

    private static byte[] Base32(string s)
    {
        const string abc = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0; var val = 0; var out_ = new List<byte>();
        foreach (var c in s.TrimEnd('=').ToUpperInvariant())
        {
            val = (val << 5) | abc.IndexOf(c); bits += 5;
            if (bits >= 8) { out_.Add((byte)(val >> (bits - 8))); bits -= 8; }
        }
        return out_.ToArray();
    }
}
