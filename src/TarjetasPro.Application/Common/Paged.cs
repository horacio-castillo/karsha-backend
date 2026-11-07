namespace TarjetasPro.Application.Common;

/// <summary>Página de resultados: Total es el conteo con los filtros aplicados, sin paginar.</summary>
public record Paged<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize)
{
    public const int DefaultSize = 20;
    public const int MaxSize = 100;

    /// <summary>Acota los valores del cliente: página mínima 1 y tamaño entre 1 y <see cref="MaxSize"/>.</summary>
    public static (int Page, int Size) Normalize(int? page, int? pageSize) =>
        (Math.Max(1, page ?? 1), Math.Clamp(pageSize ?? DefaultSize, 1, MaxSize));
}
