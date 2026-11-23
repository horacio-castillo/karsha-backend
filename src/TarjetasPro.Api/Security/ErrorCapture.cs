using System.Security.Claims;
using TarjetasPro.Infrastructure.Logging;

namespace TarjetasPro.Api.Security;

/// <summary>Atrapa cualquier excepción no controlada, la guarda en «Logs» y responde algo entendible en vez de un 500 mudo.</summary>
public class ErrorCaptureMiddleware(RequestDelegate next, SystemLogger log)
{
    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await next(ctx); }
        catch (OperationCanceledException) when (ctx.RequestAborted.IsCancellationRequested) { }
        catch (Exception ex)
        {
            Guid? uid = Guid.TryParse(ctx.User.FindFirstValue("sub"), out var g) ? g : null;
            await log.ErrorAsync("api", ex, ctx.Request.Path.Value, ctx.Request.Method, uid, ctx.Request.Headers.UserAgent.ToString());
            if (ctx.Response.HasStarted) throw;
            ctx.Response.Clear();
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await ctx.Response.WriteAsJsonAsync(new { errors = new[] { "Algo salió mal de nuestro lado. Ya quedó registrado y lo revisaremos." } });
        }
    }
}
