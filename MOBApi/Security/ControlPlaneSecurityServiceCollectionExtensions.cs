// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace Moba.MOBApi.Security;

/// <summary>
/// Registers the additive control-plane authentication and credential foundation.
/// </summary>
public static class ControlPlaneSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddControlPlaneSecurity(
        this IServiceCollection services,
        IConfiguration configuration,
        HostBootstrapMaterial? hostBootstrapMaterial = null)
    {
        services.AddOptions<ControlPlaneSecurityOptions>()
            .Bind(configuration.GetSection(ControlPlaneSecurityOptions.SectionName))
            .Validate(ValidateOptions, "Control-plane security durations and limits must be positive.")
            .ValidateOnStart();
        services.AddDataProtection().SetApplicationName("MOBApi.ControlPlane");
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IValidateOptions<ControlPlaneSecurityOptions>, ControlPlaneSecurityOptionsValidator>();
        services.AddSingleton(hostBootstrapMaterial ?? HostBootstrapMaterial.Unavailable);
        services.AddSingleton<IControlPlaneConnectionRevoker, ControlPlaneConnectionRevoker>();
        services.AddSingleton<IControlPlaneHubConnectionRegistry, ControlPlaneHubConnectionRegistry>();
        services.AddSingleton<IHostCredentialService, HostCredentialService>();
        services.AddSingleton<ICredentialRegistry, CredentialRegistry>();
        services.AddSingleton<IControlPlaneAccessTokenService, ControlPlaneAccessTokenService>();
        services.AddSingleton<IServerIdentityProvider, ServerIdentityProvider>();
        services.AddSingleton<IPairingService, PairingService>();
        services.AddSingleton<CompatibilityReadTelemetry>();
        // The read-only and recorder interfaces intentionally share one aggregate counter instance.
        services.AddSingleton<ICompatibilityReadTelemetry>(serviceProvider =>
            serviceProvider.GetRequiredService<CompatibilityReadTelemetry>());
        services.AddSingleton<ICompatibilityReadTelemetryRecorder>(serviceProvider =>
            serviceProvider.GetRequiredService<CompatibilityReadTelemetry>());
        services.AddSingleton<ICompatibilityReadiness, CompatibilityReadiness>();
        services.AddHostedService<AnonymousReadRollbackStartupReporter>();
        services.AddSingleton<IAuthorizationHandler, LiveCapabilityAuthorizationHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, CompatibilityReadAuthorizationResultHandler>();

        services.AddAuthentication(ControlPlaneAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, ControlPlaneAuthenticationHandler>(
                ControlPlaneAuthenticationDefaults.Scheme,
                _ => { });
        services.AddAuthorization(options =>
        {
            foreach (var capability in ControlPlaneCapabilities.All)
            {
                options.AddPolicy(capability, policy =>
                {
                    policy.AddRequirements(new LiveCapabilityRequirement(
                        capability,
                        AllowAnonymousCompatibility: capability == ControlPlaneCapabilities.Read));
                });
            }
        });
        return services;
    }

    private static bool ValidateOptions(ControlPlaneSecurityOptions options) =>
        options.AccessTokenLifetime > TimeSpan.Zero &&
        options.RefreshInactivityLifetime > TimeSpan.Zero &&
        options.RefreshAbsoluteLifetime > TimeSpan.Zero &&
        options.PairingWindowLifetime > TimeSpan.Zero &&
        options.PairingCooldown > TimeSpan.Zero &&
        options.PairingMaximumFailedAttempts > 0 &&
        options.HostBootstrapLifetime > TimeSpan.Zero &&
        options.HostBootstrapMaximumFailedAttempts > 0 &&
        options.HostDisconnectGrace > TimeSpan.Zero;
}