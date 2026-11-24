// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Net.Http.Headers;

namespace Moba.Demo;

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

    /// <summary>
    /// Whether Strict-Transport-Security is sent: only over HTTPS and never for a loopback host, so a local run
    /// cannot pin the browser to HTTPS for <c>localhost</c>.
    /// </summary>
    internal static bool ShouldSendHsts(bool isHttps, HostString host)
    {
        if (!isHttps)
        {
            return false;
        }

        var name = host.Host.Trim('[', ']');
        return !(name.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
            || (System.Net.IPAddress.TryParse(name, out var address) && System.Net.IPAddress.IsLoopback(address)));
    }

    /// <summary>Adds the security headers, the default cache policy and the neutral bodies of error answers to every response.</summary>
    internal static void UseResponsePolicy(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(static state =>
            {
                var response = (HttpResponse)state;
                ApplySecurityHeaders(response);
                if (response.StatusCode >= StatusCodes.Status400BadRequest)
                {
                    StripVariantHeaders(response.Headers);
                }

                ApplyDefaultCacheControl(response.Headers);
                return Task.CompletedTask;
            }, context.Response);
            await next(context).ConfigureAwait(false);
        });

        app.UseStatusCodePages(context =>
        {
            var response = context.HttpContext.Response;
            response.ContentType = "text/plain";
            return response.WriteAsync(ReasonPhrases.GetReasonPhrase(response.StatusCode));
        });
    }

    /// <summary>Answers every method other than GET and HEAD with the neutral 405; runs after the request limiter, so such requests are counted.</summary>
    internal static void UseReadOnlyMethods(this WebApplication app)
    {
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

    private static void ApplySecurityHeaders(HttpResponse response)
    {
        var headers = response.Headers;
        headers.ContentSecurityPolicy = ContentSecurityPolicy;
        headers.XContentTypeOptions = "nosniff";
        headers["Referrer-Policy"] = "no-referrer";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=()";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        if (ShouldSendHsts(response.HttpContext.Request.IsHttps, response.HttpContext.Request.Host))
        {
            headers.StrictTransportSecurity = "max-age=31536000";
        }

        if (response.ContentType?.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) == true)
        {
            headers["Cross-Origin-Opener-Policy"] = "same-origin";
        }
    }

    // An error answer belongs to no file variant and must never be cached as one.
    private static void StripVariantHeaders(IHeaderDictionary headers)
    {
        headers.Remove(HeaderNames.ContentEncoding);
        headers.Remove(HeaderNames.ETag);
        headers.Remove(HeaderNames.LastModified);
        if (headers.CacheControl.ToString().Contains("immutable", StringComparison.Ordinal))
        {
            headers.Remove(HeaderNames.CacheControl);
        }
    }

    private static void ApplyDefaultCacheControl(IHeaderDictionary headers)
    {
        if (!headers.ContainsKey(HeaderNames.CacheControl))
        {
            headers[HeaderNames.CacheControl] = "no-cache";
        }
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
                        RestrictConditionalToVariant(context.Request, chosen.Value.Encoding);
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
            var response = context.Context.Response;
            if (response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status206PartialContent or StatusCodes.Status304NotModified
                && context.Context.Request.Path.StartsWithSegments(AssetsPrefix, StringComparison.Ordinal))
            {
                response.Headers.CacheControl = "public, max-age=31536000, immutable";
            }

            // The static-file ETag comes from the served file's size and time, so equal variants could collide:
            // name the coding in the tag. RestrictConditionalToVariant maps incoming tags back.
            var coding = response.Headers.ContentEncoding.ToString();
            var tag = response.Headers.ETag.ToString();
            if (coding.Length > 0 && tag.EndsWith('"'))
            {
                response.Headers.ETag = tag[..^1] + "-" + coding + "\"";
            }
        },
    };

    // A tag that matches no file tag: the static-file middleware compares with the tags of the served file, which are
    // hexadecimal digits only.
    private const string NeverMatchingTag = "\"-\"";

    // A conditional request for a variant can only match a tag that names that variant's coding. Tags of other
    // variants (or of the uncompressed file) never match; matching tags lose the coding suffix so that the
    // static-file middleware compares them with the file's own tag.
    private static void RestrictConditionalToVariant(HttpRequest request, string coding)
    {
        var suffix = "-" + coding + "\"";
        var headers = request.Headers;
        var typed = request.GetTypedHeaders();
        if (headers.ContainsKey(HeaderNames.IfNoneMatch))
        {
            var kept = ToFileTags(typed.IfNoneMatch, suffix, strongOnly: false);
            if (kept.Count == 0)
            {
                headers.Remove(HeaderNames.IfNoneMatch);
            }
            else
            {
                headers.IfNoneMatch = string.Join(", ", kept);
            }

            // RFC 9110 13.2.2: with If-None-Match present If-Modified-Since is ignored; dropping a tag that cannot
            // match must not let If-Modified-Since answer 304 instead.
            headers.Remove(HeaderNames.IfModifiedSince);
        }

        if (headers.ContainsKey(HeaderNames.IfMatch))
        {
            var kept = ToFileTags(typed.IfMatch, suffix, strongOnly: true);
            headers.IfMatch = kept.Count == 0 ? NeverMatchingTag : string.Join(", ", kept);
        }

        if (headers.TryGetValue(HeaderNames.IfRange, out var range))
        {
            headers.IfRange = MapIfRange(range.ToString(), suffix);
        }
    }

    // If-Match uses strong comparison (RFC 9110 13.1.1), so a weak tag is skipped there; If-None-Match compares weakly.
    private static List<string> ToFileTags(IList<EntityTagHeaderValue> tags, string suffix, bool strongOnly)
    {
        var kept = new List<string>();
        foreach (var tag in tags)
        {
            if (strongOnly && tag.IsWeak)
            {
                continue;
            }

            var value = tag.Tag.Value ?? string.Empty;
            if (value == "*")
            {
                kept.Add("*");
            }
            else if (value.EndsWith(suffix, StringComparison.Ordinal))
            {
                kept.Add(value[..^suffix.Length] + "\"");
            }
        }

        return kept;
    }

    // If-Range carries either a tag or a date; only a tag needs mapping.
    private static string MapIfRange(string value, string suffix)
    {
        var trimmed = value.Trim();
        if (!trimmed.StartsWith('"') && !trimmed.StartsWith("W/", StringComparison.Ordinal))
        {
            return value;
        }

        return trimmed.EndsWith(suffix, StringComparison.Ordinal) ? trimmed[..^suffix.Length] + "\"" : NeverMatchingTag;
    }

    // RFC 9110 12.5.3: the coding with the highest quality wins, ties go to Brotli, then gzip, then identity.
    // Identity is acceptable unless it is excluded by "identity;q=0" or by "*;q=0" without an identity entry. When
    // nothing at all is acceptable the server may answer 406 or send the file as it is; the host sends the file
    // uncompressed (null result), as it also does when the client sends no Accept-Encoding header.
    private static (string Extension, string Encoding)? Choose(
        IList<StringWithQualityHeaderValue> accepted,
        (string Extension, string Encoding)[] offered)
    {
        (string Extension, string Encoding)? best = null;
        var bestQuality = 0.0;
        foreach (var variant in offered)
        {
            var quality = QualityOf(accepted, variant.Encoding, defaultQuality: 0.0);
            if (quality > bestQuality)
            {
                best = variant;
                bestQuality = quality;
            }
        }

        // An identity that is not listed (and not covered by "*") is the least preferred fallback: any listed coding
        // with a positive quality beats it, but it still wins over a coding refused with q=0.
        return bestQuality >= QualityOf(accepted, "identity", defaultQuality: double.Epsilon) ? best : null;
    }

    private static double QualityOf(IList<StringWithQualityHeaderValue> accepted, string encoding, double defaultQuality)
    {
        double? wildcard = null;
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

        return wildcard ?? defaultQuality;
    }
}
