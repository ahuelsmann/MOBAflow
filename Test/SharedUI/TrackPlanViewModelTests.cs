// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.

namespace Moba.Test.SharedUI;

using global::Moba.Backend.Service.TrackPlan;
using global::Moba.Common.Configuration;
using global::Moba.SharedUI.Interface;
using global::Moba.SharedUI.Service;
using global::Moba.SharedUI.ViewModel;
using global::Moba.TrackLibrary.PikoA;
using global::Moba.TrackPlan.Renderer;

using Microsoft.Extensions.Logging;

using Moq;

internal sealed class TrackPlanViewModelTests
{
    [Test]
    public void Selection_Should_ProjectSummaryAndCommandAvailability()
    {
        var (viewModel, plan) = CreateViewModel();
        var segment = new PlacedSegment(new G231(), 12, 34, 15);
        plan.AddSegment(segment);

        viewModel.SelectTrack(segment.Segment.No);

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.SelectedTrackId, Is.EqualTo(segment.Segment.No));
            Assert.That(viewModel.SelectionSummary, Does.Contain("G231"));
            Assert.That(viewModel.SelectionSummary, Does.Contain("X=12 mm"));
            Assert.That(viewModel.CanDeleteSelectedTrack, Is.True);
            Assert.That(viewModel.CanRotateSelectedTrack, Is.True);
        });
    }

    [Test]
    public void UndoAndRedoCommands_Should_UpdateStatusAndHistoryState()
    {
        var (viewModel, plan) = CreateViewModel();
        viewModel.PlaceSegment(new PlacedSegment(new G231(), 0, 0, 0), snapEnabled: false);

        viewModel.Undo();
        var undoStatus = viewModel.StatusText;
        viewModel.Redo();

        Assert.Multiple(() =>
        {
            Assert.That(undoStatus, Is.EqualTo("Undo executed."));
            Assert.That(viewModel.StatusText, Is.EqualTo("Redo executed."));
            Assert.That(plan.Segments, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task ValidateAsync_Should_UseDialogServiceAndExposeMessages()
    {
        var dialogService = new Mock<IDialogService>();
        dialogService
            .Setup(service => service.ShowConfirmationAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()))
            .ReturnsAsync(false);
        var (viewModel, plan) = CreateViewModel(dialogService.Object);
        plan.AddSegment(new PlacedSegment(new G231(), 0, 0, 0));

        await viewModel.ValidateAsync();

        Assert.Multiple(() =>
        {
            Assert.That(viewModel.ValidationMessages, Has.Some.Contains("open track ends"));
            Assert.That(viewModel.StatusText, Does.StartWith("Validation completed:"));
            dialogService.Verify(service => service.ShowConfirmationAsync(
                "Validation Completed",
                It.Is<string>(message => message.Contains("open track ends", StringComparison.Ordinal)),
                "OK",
                "Cancel",
                false), Times.Once);
        });
    }

    private static (TrackPlanViewModel ViewModel, EditableTrackPlan Plan) CreateViewModel(
        IDialogService? dialogService = null)
    {
        var plan = new EditableTrackPlan();
        var editorService = new TrackPlanEditorService(
            plan,
            new TrackPlanInteractionService(plan),
            new SelectionService(),
            new UndoRedoService<TrackPlanEditorDocument>());
        var viewModel = new TrackPlanViewModel(
            new TrackPlan(),
            editorService,
            dialogService ?? Mock.Of<IDialogService>(),
            new AppSettings(),
            Mock.Of<ISettingsService>(),
            Mock.Of<ILogger<TrackPlanViewModel>>());
        return (viewModel, plan);
    }
}
