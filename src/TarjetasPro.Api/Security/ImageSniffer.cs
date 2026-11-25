using Microsoft.AspNetCore.Http;
using SkiaSharp;

namespace TarjetasPro.Api.Security;

/// <summary>Valida imágenes por su contenido real (no por nombre ni Content-Type), las aligera y las guarda con nombre aleatorio.</summary>
public static class ImageSniffer
{
    public record SaveResult(string? FileName, string? Error);

    /// <summary>Lado mayor máximo tras subir. De sobra para una tarjeta de 390 puntos en pantallas 3x.</summary>
    public const int MaxSide = 1600;

    public static async Task<SaveResult> SaveAsync(IFormFile? file, string directory, long maxBytes)
    {
        if (file is null || file.Length == 0) return new(null, "Archivo vacío.");
        if (file.Length > maxBytes) return new(null, $"La imagen no puede pesar más de {maxBytes / 1024 / 1024} MB.");

        byte[] bytes;
        await using (var input = file.OpenReadStream())
        using (var ms = new MemoryStream((int)file.Length)) { await input.CopyToAsync(ms); bytes = ms.ToArray(); }

        var ext = DetectExtension(bytes.AsSpan(0, Math.Min(12, bytes.Length)));
        if (ext is null) return new(null, "Formato no válido. Usa JPG, PNG o WEBP.");

        var (outBytes, outExt) = Shrink(bytes, ext);
        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}{outExt}";
        await File.WriteAllBytesAsync(Path.Combine(directory, name), outBytes);
        return new(name, null);
    }

    /// <summary>
    /// Endereza la foto según su EXIF, la reduce a <see cref="MaxSide"/> y la recomprime:
    /// JPG (calidad 82) si es opaca, WEBP si tiene transparencia. Si el resultado no mejora, se queda la original.
    /// </summary>
    public static (byte[] Bytes, string Ext) Shrink(byte[] bytes, string ext)
    {
        try
        {
            using var codec = SKCodec.Create(new SKMemoryStream(bytes));
            if (codec is null) return (bytes, ext);
            using var decoded = SKBitmap.Decode(codec);
            if (decoded is null) return (bytes, ext);

            var origin = codec.EncodedOrigin;
            var turned = origin is SKEncodedOrigin.LeftTop or SKEncodedOrigin.RightTop or SKEncodedOrigin.RightBottom or SKEncodedOrigin.LeftBottom;
            int w = turned ? decoded.Height : decoded.Width, h = turned ? decoded.Width : decoded.Height;
            var scale = Math.Min(1.0, (double)MaxSide / Math.Max(w, h));
            var needsWork = scale < 1 || origin != SKEncodedOrigin.TopLeft;

            var alpha = decoded.AlphaType != SKAlphaType.Opaque && HasTransparency(decoded);
            int tw = Math.Max(1, (int)Math.Round(w * scale)), th = Math.Max(1, (int)Math.Round(h * scale));
            using var surface = SKSurface.Create(new SKImageInfo(tw, th, SKColorType.Rgba8888, SKAlphaType.Premul));
            var c = surface.Canvas;
            c.Clear(alpha ? SKColors.Transparent : SKColors.White);
            c.Scale((float)scale);
            c.Concat(OriginMatrix(origin, decoded.Width, decoded.Height));
            using (var img = SKImage.FromBitmap(decoded))
                c.DrawImage(img, 0, 0, new SKSamplingOptions(SKCubicResampler.Mitchell));
            using var snap = surface.Snapshot();
            using var data = alpha ? snap.Encode(SKEncodedImageFormat.Webp, 85) : snap.Encode(SKEncodedImageFormat.Jpeg, 82);
            if (data is null) return (bytes, ext);
            var result = data.ToArray();
            if (!needsWork && result.Length >= bytes.Length) return (bytes, ext);
            return (result, alpha ? ".webp" : ".jpg");
        }
        catch
        {
            return (bytes, ext); // ante cualquier rareza del decodificador, se guarda tal cual (ya se validó el formato)
        }
    }

    private static bool HasTransparency(SKBitmap b)
    {
        // Muestreo: muchas PNG declaran alfa sin usarlo; esas pueden ir como JPG.
        var step = Math.Max(1, Math.Max(b.Width, b.Height) / 200);
        for (var y = 0; y < b.Height; y += step)
            for (var x = 0; x < b.Width; x += step)
                if (b.GetPixel(x, y).Alpha < 250) return true;
        return false;
    }

    /// <summary>Matriz que lleva la imagen cruda (w×h) a su orientación correcta según EXIF.</summary>
    private static SKMatrix OriginMatrix(SKEncodedOrigin o, int w, int h) => o switch
    {
        SKEncodedOrigin.TopRight => new SKMatrix(-1, 0, w, 0, 1, 0, 0, 0, 1),
        SKEncodedOrigin.BottomRight => new SKMatrix(-1, 0, w, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.BottomLeft => new SKMatrix(1, 0, 0, 0, -1, h, 0, 0, 1),
        SKEncodedOrigin.LeftTop => new SKMatrix(0, 1, 0, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightTop => new SKMatrix(0, -1, h, 1, 0, 0, 0, 0, 1),
        SKEncodedOrigin.RightBottom => new SKMatrix(0, -1, h, -1, 0, w, 0, 0, 1),
        SKEncodedOrigin.LeftBottom => new SKMatrix(0, 1, 0, -1, 0, w, 0, 0, 1),
        _ => SKMatrix.Identity,
    };

    private static string? DetectExtension(ReadOnlySpan<byte> h) =>
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF ? ".jpg" :
        h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 ? ".png" :
        h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
            h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P' ? ".webp" : null;
}
