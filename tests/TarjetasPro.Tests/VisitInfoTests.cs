using System.Net;
using TarjetasPro.Infrastructure.Analytics;

namespace TarjetasPro.Tests;

/// <summary>Origen y dispositivo de cada visita para la tabla «Visitas» del admin.</summary>
public class VisitInfoTests
{
    private const string IPhoneSafari = "Mozilla/5.0 (iPhone; CPU iPhone OS 18_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/18.0 Mobile/15E148 Safari/604.1";
    private const string Desktop = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0 Safari/537.36";

    [Theory]
    [InlineData("qr", null, Desktop, "Código QR")]
    [InlineData(null, "https://l.facebook.com/l.php?u=x", Desktop, "Facebook")]
    [InlineData(null, "https://www.google.com/", Desktop, "Google")]
    [InlineData(null, "https://web.whatsapp.com/", Desktop, "WhatsApp")]
    [InlineData(null, "https://t.co/abc", Desktop, "X (Twitter)")]
    [InlineData(null, null, Desktop, "Directo")]
    [InlineData(null, "https://karsha.mx/otra", Desktop, "Directo")]
    [InlineData(null, null, IPhoneSafari + " Instagram 300.0", "Instagram")]
    [InlineData(null, null, IPhoneSafari + " [FBAN/FBIOS;FBAV/400.0]", "Facebook")]
    [InlineData(null, "https://blog.ejemplo.mx/post", Desktop, "blog.ejemplo.mx")]
    public void Source(string? src, string? referrer, string ua, string expected) =>
        Assert.Equal(expected, VisitInfo.Source(src, referrer, ua));

    [Theory]
    [InlineData(IPhoneSafari, "Móvil · iPhone")]
    [InlineData(Desktop, "Computadora · Windows")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-S918B) AppleWebKit/537.36 Chrome/131.0 Mobile Safari/537.36", "Móvil · Android")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; SM-X710) AppleWebKit/537.36 Chrome/131.0 Safari/537.36", "Tablet · Android")]
    [InlineData("", "Desconocido")]
    public void Device(string ua, string expected) => Assert.Equal(expected, VisitInfo.Device(ua));

    [Theory]
    [InlineData("187.190.10.5", true)]
    [InlineData("10.0.0.4", false)]
    [InlineData("192.168.1.20", false)]
    [InlineData("127.0.0.1", false)]
    [InlineData("2806:2f0:9000::1", true)]
    [InlineData("no-es-ip", false)]
    public void IsPublic(string ip, bool expected) => Assert.Equal(expected, VisitInfo.IsPublic(ip));

    [Fact]
    public void CleanIp_UnmapsIPv4() =>
        Assert.Equal("187.190.10.5", VisitInfo.CleanIp(IPAddress.Parse("187.190.10.5").MapToIPv6()));

    [Theory]
    [InlineData(IPhoneSafari, false)]
    [InlineData(Desktop, false)]
    [InlineData("Mozilla/5.0 (compatible; Googlebot/2.1; +http://www.google.com/bot.html)", true)]
    [InlineData("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 HeadlessChrome/131.0 Safari/537.36", true)]
    [InlineData("", true)]
    public void IsBot(string ua, bool expected) => Assert.Equal(expected, VisitInfo.IsBot(ua));

    [Theory]
    [InlineData("/", "/")]
    [InlineData("/Registro?src=fb", "/registro")]
    [InlineData("/tarjetas-digitales/#precios", "/tarjetas-digitales")]
    [InlineData(null, "/")]
    [InlineData("menu", "/menu")]
    public void CleanPath(string? path, string expected) => Assert.Equal(expected, VisitInfo.CleanPath(path));

    [Fact]
    public void Source_NavegacionInterna() => Assert.Equal("Navegó en KARSHA", VisitInfo.Source("in", null, Desktop));
}
