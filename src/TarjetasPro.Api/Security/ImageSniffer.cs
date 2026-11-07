using Microsoft.AspNetCore.Http;

namespace TarjetasPro.Api.Security;

/// <summary>Valida imágenes por su contenido real (no por nombre ni Content-Type) y las guarda con nombre aleatorio.</summary>
public static class ImageSniffer
{
    public record SaveResult(string? FileName, string? Error);

    public static async Task<SaveResult> SaveAsync(IFormFile? file, string directory, long maxBytes)
    {
        if (file is null || file.Length == 0) return new(null, "Archivo vacío.");
        if (file.Length > maxBytes) return new(null, $"La imagen no puede pesar más de {maxBytes / 1024 / 1024} MB.");

        var header = new byte[12];
        await using var input = file.OpenReadStream();
        var read = await input.ReadAsync(header);
        var ext = DetectExtension(header.AsSpan(0, read));
        if (ext is null) return new(null, "Formato no válido. Usa JPG, PNG o WEBP.");

        Directory.CreateDirectory(directory);
        var name = $"{Guid.NewGuid():N}{ext}";
        input.Position = 0;
        await using var output = File.Create(Path.Combine(directory, name));
        await input.CopyToAsync(output);
        return new(name, null);
    }

    private static string? DetectExtension(ReadOnlySpan<byte> h) =>
        h.Length >= 3 && h[0] == 0xFF && h[1] == 0xD8 && h[2] == 0xFF ? ".jpg" :
        h.Length >= 8 && h[0] == 0x89 && h[1] == 0x50 && h[2] == 0x4E && h[3] == 0x47 ? ".png" :
        h.Length >= 12 && h[0] == 'R' && h[1] == 'I' && h[2] == 'F' && h[3] == 'F' &&
            h[8] == 'W' && h[9] == 'E' && h[10] == 'B' && h[11] == 'P' ? ".webp" : null;
}
