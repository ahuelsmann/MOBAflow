// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Backend;

using global::Moba.Backend;
using global::Moba.Backend.Interface;

using Microsoft.Extensions.DependencyInjection;

internal sealed class TimetableDependencyInjectionTests
{
    [Test]
    public void AddMobaBackendServices_Should_RegisterTimetableServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMobaBackendServices();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(services, Has.Some.Matches<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(ITimetableEvaluationService)));
            Assert.That(services, Has.Some.Matches<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(ITimetableTimingService)));
            Assert.That(services, Has.Some.Matches<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(ITimetableStateStore)));
            Assert.That(services, Has.Some.Matches<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(ITimetableOperationsService)));
            Assert.That(services, Has.Some.Matches<ServiceDescriptor>(descriptor => descriptor.ServiceType == typeof(ITimetableRuntimeProjectionService)));
        });
    }
}
