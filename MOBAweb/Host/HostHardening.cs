// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Net.Http.Headers;

namespace Moba.Web;

/// <summary>
/// Response policy of the demo host: security headers, cache headers, precompressed file selection and the
/// neutral error answers (FR-058, FR-059, FR-070 to FR-073).
/// </summary>
internal static class HostHardening
{
    private const string ContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; connect-src 'self'; "
        + "base-uri 'none'; form-action 'none'; frame-ancestors 'none'; object-src 'none'";

    private const string AssetsPrefix = "/assets";
    private const string AllowedMethods = "GET, HEAD";

    private static readonly (string Extension, string Encoding)[] Variants = [(".br", "br"), (".gz", "gzip")];

    /// <summary>Content types are looked up by the name of the original file, so <c>app.js.br</c> is <c>text/javascript</c>.</summary>
    internal sealed class OriginalTypeProvider : IContentTypeProvider
    {
        private readonly FileExtensionContentTypeProvider _inner = new();

        public bool TryGetContentType(string subpath, out string contentType)
        {
            foreach (var (extension, _) in Variants)
            {
                if (subpath.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                {
                    subpath = subpath[..^extension.Length];
                    break;
                }
            }

            return _inner.TryGetContentType(subpath, out contentType!);
        }
    }

    /// <summary>Adds the security headers, the default cache policy and the neutral 404 and 405 bodies to every response.</summary>
    internal static void UseResponsePolicy(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                var headers = response.Headers;
                headers.ContentSecurityPolicy = ContentSecurityPolicy;
                headers.XContentTypeOptions = "nosniff";
                headers["Referrer-Policy"] = "no-referrer";
                headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
                headers.StrictTransportSecurity = "max-age=31536000";
                headers["Cross-Origin-Resource-Policy"] = "same-origin";
                if (response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
                {
                    headers["Cross-Origin-Opener-Policy"] = "same-origin";
                }

                if (!headers.ContainsKey(HeaderNames.CacheControl))
                {
                    headers[HeaderNames.CacheControl] = "no-cache";
                }

                return Task.CompletedTask;
            }, context.Response);
            await next(context).ConfigureAwait(false);
        });

        app.UseStatusCodePages(context =>
        {
            var response = context.HttpContext.Response;
            response.ContentType = "text/plain";
            return response.WriteAsync(response.StatusCode == StatusCodes.Status404NotFound
                ? "Not Found"
                : "Method Not Allowed");
        });

        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
            {
                await next(context).ConfigureAwait(false);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status405MethodNotAllowed;
            context.Response.Headers.Allow = AllowedMethods;
        });
    }

    /// <summary>Serves a <c>.br</c> or <c>.gz</c> sibling of a requested file when the client accepts it; never compresses at run time.</summary>
    internal static void UsePrecompressedFiles(this WebApplication app)
    {
        var files = app.Environment.WebRootFileProvider;
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path;
            if (Variants.Any(v => (path.Value ?? string.Empty).EndsWith(v.Extension, StringComparison.OrdinalIgnoreCase)))
            {
                // A compressed sibling is not a resource of its own.
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }

            var original = files.GetFileInfo(path.Value!);
            if (original.Exists && !original.IsDirectory)
            {
                var offered = Variants.Where(v => files.GetFileInfo(path.Value + v.Extension).Exists).ToArray();
                if (offered.Length > 0)
                {
                    context.Response.Headers.Append(HeaderNames.Vary, HeaderNames.AcceptEncoding);
                    var chosen = Choose(context.Request.GetTypedHeaders().AcceptEncoding, offered);
                    if (chosen != null)
                    {
                        context.Request.Path = path + chosen.Value.Extension;
                        context.Response.Headers.ContentEncoding = chosen.Value.Encoding;
                    }
                }
            }

            await next(context).ConfigureAwait(false);
        });
    }

    /// <summary>Static file options: long cache for content-hashed assets, original content types for variants.</summary>
    internal static StaticFileOptions CreateStaticFileOptions() => new()
    {
        ContentTypeProvider = new OriginalTypeProvider(),
        OnPrepareResponse = context =>
        {
            if (context.Context.Request.Path.StartsWithSegments(AssetsPrefix, StringComparison.Ordinal))
            {
                context.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
            }
        },
    };

    private static (string Extension, string Encoding)? Choose(
        IList<StringWithQualityHeaderValue> accepted,
        (string Extension, string Encoding)[] offered)
    {
        (string Extension, string Encoding)? best = null;
        var bestQuality = 0.0;
        foreach (var variant in offered)
        {
            var quality = QualityOf(accepted, variant.Encoding);
            if (quality > bestQuality)
            {
                best = variant;
                bestQuality = quality;
            }
        }

        return best;
    }

    private static double QualityOf(IList<StringWithQualityHeaderValue> accepted, string encoding)
    {
        var wildcard = 0.0;
        foreach (var item in accepted)
        {
            var quality = item.Quality ?? 1.0;
            if (string.Equals(item.Value.Value, encoding, StringComparison.OrdinalIgnoreCase))
            {
                return quality;
            }

            if (item.Value == "*")
            {
                wildcard = quality;
            }
        }

        return wildcard;
    }
}
