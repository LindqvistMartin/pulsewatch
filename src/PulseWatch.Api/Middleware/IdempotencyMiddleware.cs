using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace PulseWatch.Api.Middleware;

public sealed class IdempotencyMiddleware(RequestDelegate next, IMemoryCache cache)
{
    private const string IdempotencyHeader = "Idempotency-Key";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    public async Task InvokeAsync(HttpContext ctx)
    {
        if (!ctx.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase)
            || !ctx.Request.Headers.TryGetValue(IdempotencyHeader, out var key)
            || string.IsNullOrWhiteSpace(key))
        {
            await next(ctx);
            return;
        }

        // Enable body buffering so we can read it for the hash
        ctx.Request.EnableBuffering();
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        ctx.Request.Body.Position = 0;

        var bodyHash = Convert.ToHexString(SHA256.HashData(ms.ToArray()));
        var cacheKey = $"idempotency:{ctx.Request.Method}:{ctx.Request.Path}:{key}:{bodyHash}";

        if (cache.TryGetValue(cacheKey, out CachedResponse? cached) && cached is not null)
        {
            ctx.Response.StatusCode = cached.StatusCode;
            ctx.Response.ContentType = cached.ContentType;
            await ctx.Response.Body.WriteAsync(cached.Body);
            return;
        }

        var originalBody = ctx.Response.Body;
        using var capture = new MemoryStream();
        ctx.Response.Body = capture;

        await next(ctx);

        capture.Position = 0;
        var responseBytes = capture.ToArray();

        // Only cache successful responses
        if (ctx.Response.StatusCode is >= 200 and < 300)
        {
            cache.Set(cacheKey, new CachedResponse(ctx.Response.StatusCode,
                ctx.Response.ContentType ?? "application/json", responseBytes), Ttl);
        }

        capture.Position = 0;
        ctx.Response.Body = originalBody;
        await capture.CopyToAsync(ctx.Response.Body);
    }

    private sealed record CachedResponse(int StatusCode, string ContentType, byte[] Body);
}
