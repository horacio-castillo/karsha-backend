using SkiaSharp;
using TarjetasPro.Api.Security;
using Xunit;

namespace TarjetasPro.Tests;

public class ImageShrinkTests
{
    private static byte[] Make(int w, int h, SKEncodedImageFormat fmt, bool transparent, int q = 100)
    {
        using var surface = SKSurface.Create(new SKImageInfo(w, h, SKColorType.Rgba8888, SKAlphaType.Premul));
        surface.Canvas.Clear(transparent ? SKColors.Transparent : SKColors.White);
        using var paint = new SKPaint { Shader = SKShader.CreateLinearGradient(new(0, 0), new(w, h), [SKColors.Red, SKColors.Blue], SKShaderTileMode.Clamp) };
        surface.Canvas.DrawCircle(w / 2f, h / 2f, Math.Min(w, h) / 3f, paint);
        using var snap = surface.Snapshot();
        return snap.Encode(fmt, q).ToArray();
    }

    [Fact]
    public void FotoGrande_SeReduceYPesaMenos()
    {
        var big = Make(4000, 3000, SKEncodedImageFormat.Jpeg, false);
        var (bytes, ext) = ImageSniffer.Shrink(big, ".jpg");
        using var bmp = SKBitmap.Decode(bytes);
        Assert.Equal(".jpg", ext);
        Assert.Equal(ImageSniffer.MaxSide, Math.Max(bmp.Width, bmp.Height));
        Assert.Equal(1200, bmp.Height);
        Assert.True(bytes.Length < big.Length);
    }

    [Fact]
    public void PngConTransparencia_SeQuedaTransparente()
    {
        var png = Make(2400, 2400, SKEncodedImageFormat.Png, true);
        var (bytes, ext) = ImageSniffer.Shrink(png, ".png");
        using var bmp = SKBitmap.Decode(bytes);
        Assert.Equal(".webp", ext);
        Assert.True(bmp.GetPixel(2, 2).Alpha < 10);
    }

    [Fact]
    public void ImagenChicaYLigera_SeQuedaIgual()
    {
        var small = Make(300, 300, SKEncodedImageFormat.Jpeg, false, 60);
        var (bytes, ext) = ImageSniffer.Shrink(small, ".jpg");
        Assert.Equal(".jpg", ext);
        Assert.True(bytes.Length <= small.Length);
    }
}
