using System.Linq.Expressions;

namespace TarjetasPro.Api;

/// <summary>
/// Orden de las tablas del panel. Cada lista admite solo sus propias claves (?sort=…&amp;dir=asc|desc);
/// una clave desconocida o vacía deja el orden de siempre.
/// </summary>
public static class Sorting
{
    public static bool IsDesc(string? dir) => string.Equals(dir, "desc", StringComparison.OrdinalIgnoreCase);

    public static IOrderedQueryable<T> By<T, TKey>(this IQueryable<T> q, bool desc, Expression<Func<T, TKey>> key) =>
        desc ? q.OrderByDescending(key) : q.OrderBy(key);

    /// <summary>Ordena dejando los vacíos al final, tanto al subir como al bajar.</summary>
    public static IOrderedQueryable<T> ByEmptyLast<T, TKey>(this IQueryable<T> q, bool desc, Expression<Func<T, bool>> isEmpty, Expression<Func<T, TKey>> key)
    {
        var first = q.OrderBy(isEmpty);
        return desc ? first.ThenByDescending(key) : first.ThenBy(key);
    }
}
