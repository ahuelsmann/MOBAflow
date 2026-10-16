// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.Helpers;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Service.Validation;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;
using System.ComponentModel;

internal static class TestLocomotiveTrainUsage
{
    internal static void RaiseModelChanged(Mock<ISolutionSession> session, object sender, string propertyName)
    {
        // ModelChanged is synchronous; Moq's RaiseAsync has no Task result for PropertyChangedEventHandler.
        session.Raise(value => value.ModelChanged += null, sender, new PropertyChangedEventArgs(propertyName));
    }

    internal static Project CreateProject()
    {
        var locomotive = new Locomotive { Name = "Harbor pilot" };
        return new Project
        {
            Name = "Yard", Locomotives = [locomotive],
            Trains = [new Train { Name = "Morning service", Vehicles = [new Vehicle { VehicleKind = TrainVehicleKind.Locomotive, VehicleId = locomotive.Id }] }]
        };
    }

    internal static Mock<ISolutionSession> CreateSession(Project project)
    {
        var session = new Mock<ISolutionSession>();
        session.SetupGet(value => value.Solution).Returns(new Solution { Projects = [project] });
        session.SetupGet(value => value.SelectedProject).Returns(new ProjectViewModel(project));
        return session;
    }

    internal static Mock<ILocomotiveTrainRuntimeUsageReader> UnknownReader()
    {
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                LocomotiveTrainRuntimeObservation.Unknown(request, LocomotiveTrainCoverageReason.OwnerUnavailable));
        return reader;
    }

    internal static Mock<ILocomotiveTrainRuntimeUsageReader> CompleteReader(Project definition)
    {
        var reader = new Mock<ILocomotiveTrainRuntimeUsageReader>();
        reader.Setup(value => value.ReadAsync(It.IsAny<LocomotiveTrainUsageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((LocomotiveTrainUsageRequest request, CancellationToken _) =>
                new LocomotiveTrainRuntimeObservation(request.RequestId, request.ProjectId, DateTimeOffset.UtcNow,
                    new LocomotiveTrainReferenceAnalyzer().Analyze(definition, LocomotiveTrainSource.LoadedRuntimeDefinition, request.LocomotiveId)));
        reader.Setup(value => value.IsCurrentAsync(It.IsAny<LocomotiveTrainRuntimeObservation>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(LocomotiveTrainObservationValidity.Current);
        return reader;
    }

    internal static LocomotiveTrainUsageViewModel CreateViewModel(
        ISolutionSession session, ILocomotiveTrainRuntimeUsageReader reader, IProjectRuntimeSnapshots? snapshots = null,
        IUiDispatcher? dispatcherOverride = null)
    {
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        dispatcher.Setup(value => value.InvokeOnUiAsync(It.IsAny<Func<Task>>())).Returns<Func<Task>>(action => action());
        return new(session, new LocomotiveTrainReferenceAnalyzer(), reader,
            snapshots ?? new Mock<IProjectRuntimeSnapshots>().Object, dispatcherOverride ?? dispatcher.Object,
            NullLogger<LocomotiveTrainUsageViewModel>.Instance);
    }
}
