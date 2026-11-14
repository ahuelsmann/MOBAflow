// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.Helpers;

using Microsoft.Extensions.DependencyInjection;

/// <summary>Resolves the registrations of a host's service collection, so a container test covers all of them.</summary>
internal static class ServiceRegistrations
{
    /// <summary>
    /// Resolves every registered service type from <paramref name="provider"/>, including every implementation
    /// of a type that is registered several times, and returns one line per type that could not be created.
    /// Open generic registrations such as <c>ILogger&lt;T&gt;</c> have no instance of their own; the services
    /// that use them resolve their closed form.
    /// </summary>
    /// <param name="services">The registrations the provider was built from.</param>
    /// <param name="provider">The provider under test.</param>
    /// <param name="skip">Service types the test cannot create, for example XAML pages without a window.</param>
    /// <param name="resolved">The number of service types that were resolved.</param>
    public static IReadOnlyList<string> ResolveAll(
        IServiceCollection services, IServiceProvider provider, Func<Type, bool> skip, out int resolved)
    {
        var failures = new List<string>();
        resolved = 0;
        using var scope = provider.CreateScope();
        var registrations = services
            .Where(descriptor => !descriptor.ServiceType.IsGenericTypeDefinition && !skip(descriptor.ServiceType))
            .Select(descriptor => (descriptor.ServiceType, Key: descriptor.IsKeyedService ? descriptor.ServiceKey : null))
            .Distinct();
        foreach (var (serviceType, key) in registrations)
        {
            try
            {
                var instances = key is null
                    ? scope.ServiceProvider.GetServices(serviceType)
                    : scope.ServiceProvider.GetKeyedServices(serviceType, key);
                if (instances.Any(instance => instance is null))
                    failures.Add($"{serviceType}: a registration returned null");
                resolved++;
            }
            catch (Exception exception) when (exception is not OutOfMemoryException)
            {
                failures.Add($"{serviceType}: {exception.GetType().Name}: {exception.Message}");
            }
        }

        return failures;
    }
}
