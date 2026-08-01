// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

internal sealed class ControlPlaneSecurityOptionsValidator(TimeProvider timeProvider)
    : IValidateOptions<ControlPlaneSecurityOptions>
{
    private static readonly TimeSpan MaximumAnonymousReadRollback = TimeSpan.FromDays(7);

    public ValidateOptionsResult Validate(string? name, ControlPlaneSecurityOptions options)
    {
        if (options.AnonymousReadRollbackUntilUtc > timeProvider.GetUtcNow() + MaximumAnonymousReadRollback)
        {
            return ValidateOptionsResult.Fail(
                "Anonymous read rollback cannot remain active for more than seven days.");
        }

        return ValidateOptionsResult.Success;
    }
}