// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Moba.MOBApi.Security;

/// <summary>
/// Records authenticated REST-read evidence only after the endpoint completed successfully.
/// </summary>
internal sealed class CompatibilityReadObservationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, ICompatibilityReadMigration migration)
    {
        await next(context).ConfigureAwait(false);

        if (context.Response.StatusCode is < StatusCodes.Status200OK or >= StatusCodes.Status300MultipleChoices ||
            context.User.Identity?.IsAuthenticated != true ||
            !IsAuthenticatedReadEndpoint(context))
        {
            return;
        }

        var credentialId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(credentialId))
            return;

        await migration.RecordAuthenticatedReadAsync(
                credentialId,
                CompatibilityReadTransport.Rest,
                context.Request.Headers[CompatibilityReadHeaders.ClientRelease].FirstOrDefault(),
                CancellationToken.None)
            .ConfigureAwait(false);
    }

    private static bool IsAuthenticatedReadEndpoint(HttpContext context) =>
        !context.Request.Path.StartsWithSegments("/runtime-hub", StringComparison.Ordinal) &&
        !context.Request.Path.StartsWithSegments("/photos-hub", StringComparison.Ordinal) &&
        context.GetEndpoint()?.Metadata
            .GetOrderedMetadata<IAuthorizeData>()
            .Any(metadata => string.Equals(
                metadata.Policy,
                ControlPlaneCapabilities.Read,
                StringComparison.Ordinal)) == true;
}
