// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

[TestFixture]
[SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "NUnit creates this fixture through reflection.")]
internal sealed partial class TrainsPageViewModelTests : IDisposable
{
    private Mock<ISolutionSession> _session = null!;
    private Mock<IConnectionRuntime> _runtime = null!;
    private Mock<ISettingsService> _persistence = null!;
    private AppSettings _settings = null!;
    private TestRollingStockPages _stock = null!;
    private TrainsPageViewModel _page = null!;

    [SetUp]
    public void SetUp()
    {
        _session = new Mock<ISolutionSession>();
        _session.SetupProperty(value => value.SelectedProject, new ProjectViewModel(new Project
        {
            Trains = [new Train { Name = "Regional" }, new Train { Name = "Freight" }]
        }));
        _session.Setup(value => value.SaveSolutionInternalAsync()).Returns(Task.CompletedTask);
        _runtime = new Mock<IConnectionRuntime>();
        _runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _stock = new TestRollingStockPages();
        _page = CreatePage();
    }

    private TrainsPageViewModel CreatePage() => new(_session.Object, _runtime.Object, _persistence.Object,
        new LayoutColumnWidthsViewModel(), _stock.GetLocomotivesPage(_session.Object),
        _stock.GetWagonsPage(_session.Object), NullLogger<TrainsPageViewModel>.Instance);

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        _stock.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void SelectionAndCrud_KeepModelWrappersAndCommandAvailabilityInSync()
    {
        using var assertions = Assert.EnterMultipleScope();
        var project = _session.Object.SelectedProject!;
        Assert.That(_page.SelectedTrain, Is.SameAs(project.Trains[0]));
        Assert.That(_page.DeleteTrainCommand.CanExecute(null), Is.True);
        _page.AddTrainCommand.Execute(null);
        Assert.That(_page.SelectedTrain!.Model, Is.SameAs(project.Model.Trains[2]));
        Assert.That(_page.SelectedTrain.Name, Is.EqualTo("Train 3"));
        _page.DeleteTrainCommand.Execute(null);
        Assert.That(_page.SelectedTrain, Is.SameAs(project.Trains[0]));
        _page.DeleteTrainCommand.Execute(null);
        _page.DeleteTrainCommand.Execute(null);
        Assert.That(project.Model.Trains, Is.Empty);
        Assert.That(_page.FilteredTrains, Is.Empty);
        Assert.That(_page.SelectedTrain, Is.Null);
        Assert.That(_page.SelectedVehicles, Is.Empty);
        Assert.That(_page.DeleteTrainCommand.CanExecute(null), Is.False);
    }

    [TestCase("REG", 1)]
    [TestCase("freight", 1)]
    [TestCase("missing", 0)]
    [TestCase("  ", 2)]
    public void Search_PreservesCaseInsensitiveAndWhitespaceBehavior(string search, int count)
    {
        _page.TrainSearchText = search;
        Assert.That(_page.FilteredTrains, Has.Count.EqualTo(count));
    }

    [Test]
    public void InsertAllVehicleTypes_PreservesReferencesOrderAndRejectsDuplicates()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.LocomotivesPage.AddLocomotiveCommand.Execute(null);
        _page.WagonsPage.AddPassengerWagonCommand.Execute(null);
        _page.WagonsPage.AddGoodsWagonCommand.Execute(null);
        var locomotive = _page.LocomotivesPage.SelectedLocomotive!;
        var passenger = _page.WagonsPage.SelectedPassengerWagon!;
        var goods = _page.WagonsPage.SelectedGoodsWagon!;
        _page.AddLocomotiveToSelectedTrain(locomotive);
        _page.AddPassengerWagonToSelectedTrain(passenger);
        _page.AddGoodsWagonToSelectedTrain(goods, 1);
        _page.AddLocomotiveToSelectedTrain(locomotive, 0);
        Assert.That(_page.SelectedTrain!.Model.Vehicles.Select(value => value.VehicleId),
            Is.EqualTo(new[] { locomotive.Model.Id, goods.Model.Id, passenger.Model.Id }));
        Assert.That(_page.SelectedVehicles.Select(value => value.VehicleKind),
            Is.EqualTo(new[] { TrainVehicleKind.Locomotive, TrainVehicleKind.GoodsWagon, TrainVehicleKind.PassengerWagon }));
        _session.Verify(value => value.SaveSolutionInternalAsync(), Times.AtLeast(3));
    }

    [Test]
    public void ReorderAndRemove_PersistVehicleOrderDirectionAndRuntimeProjection()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.WagonsPage.AddGoodsWagonCommand.Execute(null);
        _page.AddGoodsWagonToSelectedTrain(_page.WagonsPage.SelectedGoodsWagon);
        _page.WagonsPage.AddPassengerWagonCommand.Execute(null);
        _page.AddPassengerWagonToSelectedTrain(_page.WagonsPage.SelectedPassengerWagon);
        var reversedId = _page.SelectedVehicles[1].VehicleId;
        _page.SelectedVehicles[1].ToggleDirectionCommand!.Execute(null);
        _page.SelectedVehicles.Move(1, 0);
        _page.SynchronizeSelectedVehicles();
        Assert.That(_page.SelectedTrain!.Model.Vehicles[0].VehicleId, Is.EqualTo(reversedId));
        Assert.That(_page.SelectedTrain.Model.Vehicles[0].IsReversed, Is.True);
        _page.RemoveSelectedVehicle(_page.SelectedVehicles[1]);
        Assert.That(_page.SelectedTrain.Model.Vehicles, Has.Count.EqualTo(1));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject!.Model, It.IsAny<CancellationToken>()), Times.AtLeast(3));
    }

    [Test]
    public void ProjectSwitch_DetachesOldCompositionAndClearsVehicleSelection()
    {
        using var assertions = Assert.EnterMultipleScope();
        var old = _page.SelectedTrain!;
        _page.SelectedVehicle = new VehicleItemViewModel(new Vehicle(), "Old");
        var project = new ProjectViewModel(new Project { Trains = [new Train { Name = "Other" }] });
        SelectProject(project);
        _session.Invocations.Clear();
        _runtime.Invocations.Clear();
        old.SynchronizeVehiclesFromItems();
        Assert.That(_page.SelectedVehicle, Is.Null);
        Assert.That(_page.SelectedTrain, Is.SameAs(project.Trains[0]));
        Assert.That(_page.TrainsCompositionTitle, Is.EqualTo("Train Composition: Other"));
        _session.Verify(value => value.SaveSolutionInternalAsync(), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
        SelectProject(null);
        _page.AddTrainCommand.Execute(null);
        Assert.That(_page.TrainsCompositionTitle, Is.EqualTo("Train Composition"));
        Assert.That(_page.FilteredTrains, Is.Empty);
    }

    [Test]
    public void SuppressedAutoSave_DoesNotSaveOrRefreshForVehicleCallbacks()
    {
        _session.SetupGet(value => value.IsAutoSaveSuppressed).Returns(true);
        _page.SelectedTrain!.SynchronizeVehiclesFromItems();
        _session.Verify(value => value.SaveSolutionInternalAsync(), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void ReplacementAndDispose_DetachCallbacksWithoutRemovingSessionOwnedTrainTracking()
    {
        using var assertions = Assert.EnterMultipleScope();
        var old = _page.SelectedTrain!;
        _session.Raise(value => value.SolutionReplacing += null, EventArgs.Empty);
        Assert.That(_page.SelectedTrain, Is.Null);
        _page.SelectedTrain = old;
        _page.Dispose();
        _page.Dispose();
        var notifications = 0;
        _page.PropertyChanged += (_, _) => notifications++;
        _session.Invocations.Clear();
        old.SynchronizeVehiclesFromItems();
        SelectProject(null);
        _session.Raise(value => value.SolutionReplacing += null, EventArgs.Empty);
        Assert.That(notifications, Is.Zero);
        Assert.That(_page.SelectedTrain, Is.SameAs(old));
        _session.Verify(value => value.SaveSolutionInternalAsync(), Times.Never);
        _session.Verify(value => value.UntrackChanges(It.IsAny<INotifyPropertyChanged>()), Times.Never);
    }

    [Test]
    public void Layout_UsesPersistedPanelsAndWritesOnlyChangedValues()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.Dispose();
        _settings.Layout.TrainsPage.IsTrainListExpanded = false;
        _settings.Layout.TrainsPage.IsLocomotiveLibraryExpanded = false;
        _settings.Layout.TrainsPage.IsPassengerLibraryExpanded = false;
        _settings.Layout.TrainsPage.IsGoodsLibraryExpanded = false;
        _settings.Layout.TrainsPage.IsPropertiesExpanded = false;
        _page = CreatePage();
        Assert.That(_page.IsTrainsListExpanded, Is.False);
        Assert.That(_page.IsTrainsLocomotiveLibraryExpanded, Is.False);
        Assert.That(_page.IsTrainsPassengerLibraryExpanded, Is.False);
        Assert.That(_page.IsTrainsGoodsLibraryExpanded, Is.False);
        Assert.That(_page.IsTrainsPropertiesExpanded, Is.False);
        _page.IsTrainsListExpanded = false;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        _page.IsTrainsListExpanded = true;
        _page.IsTrainsLocomotiveLibraryExpanded = true;
        _page.IsTrainsPassengerLibraryExpanded = true;
        _page.IsTrainsGoodsLibraryExpanded = true;
        _page.IsTrainsPropertiesExpanded = true;
        Assert.That(_settings.Layout.TrainsPage.IsTrainListExpanded, Is.True);
        Assert.That(_settings.Layout.TrainsPage.IsLocomotiveLibraryExpanded, Is.True);
        Assert.That(_settings.Layout.TrainsPage.IsPassengerLibraryExpanded, Is.True);
        Assert.That(_settings.Layout.TrainsPage.IsGoodsLibraryExpanded, Is.True);
        Assert.That(_settings.Layout.TrainsPage.IsPropertiesExpanded, Is.True);
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(5));
    }

    private void SelectProject(ProjectViewModel? project)
    {
        _session.Object.SelectedProject = project;
        _session.Raise(value => value.PropertyChanged += null, new PropertyChangedEventArgs(nameof(ISolutionSession.SelectedProject)));
    }
}
