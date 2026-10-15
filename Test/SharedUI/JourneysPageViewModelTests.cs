// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Common.Configuration;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;
using Moba.Test.Helpers;
using Moq;
using System.Text.Json;

/// <summary>Journey page editing and subscription regressions for the shared journey fixture.</summary>
public sealed partial class JourneyCounterProjectionTests : IDisposable
{
    private SolutionSession _session = null!;
    private Mock<IMobaRuntime> _runtime = null!;
    private Mock<IRuntimeCommandGateway> _gateway = null!;
    private Mock<IIoService> _io = null!;
    private Mock<ISettingsService> _persistence = null!;
    private Mock<ICityService> _cities = null!;
    private Mock<IUiDispatcher> _dispatcher = null!;
    private AppSettings _settings = null!;
    private JourneysPageViewModel _page = null!;
    private string _lastSaved = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _runtime = new Mock<IMobaRuntime>();
        _runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        _runtime.Setup(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _gateway = new Mock<IRuntimeCommandGateway>();
        _gateway.Setup(value => value.ResetJourneyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _dispatcher = new Mock<IUiDispatcher>();
        _dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        _io = new Mock<IIoService>();
        _io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .Callback((Solution solution, string _) => _lastSaved = JsonSerializer.Serialize(solution))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        _session = TestSolutionSessions.Create(new Solution { Projects = [new Project
        {
            Journeys = [new Journey { Name = "Morning", Stations = [new Station { Name = "Central" }] },
                new Journey { Name = "Evening", Stations = [new Station { Name = "East" }] }]
        }] }, _dispatcher.Object, _runtime.Object, _io.Object);
        _session.CurrentSolutionPath = "journeys-test.json";
        _settings = new AppSettings();
        _persistence = new Mock<ISettingsService>();
        _persistence.Setup(value => value.GetSettings()).Returns(_settings);
        _persistence.Setup(value => value.SaveSettingsAsync(_settings)).Returns(Task.CompletedTask);
        _cities = new Mock<ICityService>();
        _cities.Setup(value => value.LoadCitiesAsync()).ReturnsAsync([]);
        _page = CreatePage();
        ClearInvocations();
    }

    private JourneysPageViewModel CreatePage() => new(_session, _gateway.Object,
        _dispatcher.Object, _cities.Object, _persistence.Object, new LayoutColumnWidthsViewModel(),
        NullLogger<JourneysPageViewModel>.Instance);

    [TearDown]
    public void Dispose()
    {
        _page.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void JourneyCrudPreservesDefaultsSessionSelectionAndSavedModels()
    {
        using var assertions = Assert.EnterMultipleScope();
        Assert.That(_page.SelectedJourney, Is.SameAs(_session.SelectedProject!.Journeys[0]));
        Assert.That(_page.DeleteJourneyCommand.CanExecute(null), Is.True);
        _page.AddJourneyCommand.Execute(null);
        var journey = _page.SelectedJourney!;
        Assert.That(journey.Name, Is.EqualTo("New Journey"));
        Assert.That(_session.SelectedJourney, Is.SameAs(journey));
        Assert.That(_page.SelectedProject!.Model.Journeys.Last(), Is.SameAs(journey.Model));
        Assert.That(SavedProject().Journeys, Has.Count.EqualTo(3));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject.Model, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _runtime.Invocations.Clear();
        _page.DeleteJourneyCommand.Execute(null);
        Assert.That(_page.SelectedProject.Model.Journeys, Has.Count.EqualTo(2));
        Assert.That(_page.SelectedProject.Journeys, Has.Count.EqualTo(2));
        Assert.That(_session.SelectedJourney, Is.Null);
        Assert.That(_page.DeleteJourneyCommand.CanExecute(null), Is.False);
        Assert.That(SavedProject().Journeys, Has.Count.EqualTo(2));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject.Model, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        _runtime.Verify(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void StationCrudAndCityCommandsCopyOnlyTheFirstStationNameWithFreshIdentity()
    {
        using var assertions = Assert.EnterMultipleScope();
        var reference = new Station { Name = "City Central", IsExitOnLeft = true, Platforms = [new Platform { Number = 9 }] };
        var city = new City { Name = "City", Stations = [reference, new Station { Name = "Other" }] };
        _page.SelectedCity = city;
        _page.AddStationFromCityCommand.Execute(city);
        var copy = _page.SelectedStation!;
        Assert.That(copy.Model, Is.Not.SameAs(reference));
        Assert.That(copy.Model.Id, Is.Not.EqualTo(reference.Id));
        Assert.That(copy.Name, Is.EqualTo(reference.Name));
        Assert.That(copy.Model.IsExitOnLeft, Is.False);
        Assert.That(copy.Model.Platforms, Is.Empty);
        Assert.That(SavedProject().Journeys[0].Stations.Last().Name, Is.EqualTo(reference.Name));
        _page.AddStationToJourneyCommand.Execute(null);
        Assert.That(_page.SelectedJourney!.Stations, Has.Count.EqualTo(3));
        _page.AddStationCommand.Execute(null);
        Assert.That(_page.SelectedStation!.Name, Is.EqualTo("New Station"));
        _page.DeleteStationCommand.Execute(null);
        Assert.That(_page.SelectedStation, Is.Null);
        Assert.That(_page.DeleteStationCommand.CanExecute(null), Is.False);
        Assert.That(SavedProject().Journeys[0].Stations, Has.Count.EqualTo(3));
        _page.AddStationFromCityCommand.Execute(new City());
        Assert.That(_page.SelectedJourney.Stations, Has.Count.EqualTo(3));
    }

    [Test]
    public void ItemClicksUseJourneyAndStationTitlesWithoutReplacingCanonicalSelection()
    {
        using var assertions = Assert.EnterMultipleScope();
        var journey = _page.SelectedJourney!;
        _page.JourneysPageItemClickedCommand.Execute(journey);
        Assert.That(_page.JourneysPagePropertiesTitle, Is.EqualTo("Journey Properties"));
        _page.SelectedStation = journey.Stations[0];
        _page.JourneysPageItemClickedCommand.Execute(_page.SelectedStation);
        Assert.That(_page.JourneysPageSelectedObject, Is.SameAs(_page.SelectedStation));
        Assert.That(_page.JourneysPagePropertiesTitle, Is.EqualTo("Station Properties"));
        Assert.That(_session.SelectedJourney, Is.SameAs(journey));
        _page.JourneysPageItemClickedCommand.Execute(null);
        Assert.That(_page.JourneysPagePropertiesTitle, Is.EqualTo("Properties"));
        VerifyNoSavesOrRuntimeUpdates();
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ClickingAnotherJourneyPreservesItsPropertiesAcrossSelectionChange(bool clickBeforeSelection)
    {
        var oldStation = _page.SelectedJourney!.Stations[0];
        var nextJourney = _page.SelectedProject!.Journeys[1];
        _page.SelectedStation = oldStation;
        _page.JourneysPageItemClickedCommand.Execute(oldStation);

        // WinUI raises ItemClick before SelectionChanged; also tolerate the reverse order.
        // https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/listview-and-gridview#item-selection-and-interaction
        if (clickBeforeSelection) _page.JourneysPageItemClickedCommand.Execute(nextJourney);
        _session.SelectedJourney = nextJourney;
        if (!clickBeforeSelection) _page.JourneysPageItemClickedCommand.Execute(nextJourney);

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(_page.SelectedJourney, Is.SameAs(nextJourney));
        Assert.That(_session.SelectedJourney, Is.SameAs(nextJourney));
        Assert.That(_page.SelectedStation, Is.Null);
        Assert.That(_page.JourneysPageSelectedObject, Is.SameAs(nextJourney));
        Assert.That(_page.JourneysPagePropertiesTitle, Is.EqualTo("Journey Properties"));
        oldStation.Name = "Detached station";
        VerifyNoSavesOrRuntimeUpdates();
    }

    [Test]
    public void StationAndJourneyEditsAutoSaveAndRefreshTheCurrentRuntime()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.SelectedStation = _page.SelectedJourney!.Stations[0];
        _page.SelectedStation.Name = "Updated station";
        _page.SelectedJourney.Name = "Updated journey";
        Assert.That(SavedProject().Journeys[0].Stations[0].Name, Is.EqualTo("Updated station"));
        Assert.That(SavedProject().Journeys[0].Name, Is.EqualTo("Updated journey"));
        _runtime.Verify(value => value.UpdateProjectAsync(_page.SelectedProject!.Model, It.IsAny<CancellationToken>()), Times.AtLeast(2));
    }

    [Test]
    public void StationSelectionAndRefreshDetachDiscardedWrappers()
    {
        using var assertions = Assert.EnterMultipleScope();
        var journey = _page.SelectedJourney!;
        var old = journey.Stations[0];
        _page.SelectedStation = old;
        _page.JourneysPageItemClickedCommand.Execute(old);
        _page.AddStationCommand.Execute(null);
        var current = _page.SelectedStation!;
        ClearInvocations();
        old.Name = "Old selection";
        VerifyNoSavesOrRuntimeUpdates();
        _page.JourneysPageItemClickedCommand.Execute(current);
        journey.RefreshStations();
        ClearInvocations();
        current.Name = "Discarded wrapper";
        Assert.That(_page.SelectedStation, Is.Null);
        Assert.That(_page.JourneysPageSelectedObject, Is.Null);
        VerifyNoSavesOrRuntimeUpdates();
    }

    [TestCase(false)]
    [TestCase(true)]
    public void JourneySwitchObservesSessionSelectionAndDetachesPreviousStationAndJourney(bool showJourneyProperties)
    {
        using var assertions = Assert.EnterMultipleScope();
        var old = _page.SelectedJourney!;
        var station = old.Stations[0];
        _page.SelectedStation = station;
        _page.JourneysPageItemClickedCommand.Execute(showJourneyProperties ? old : station);
        _session.SelectedJourney = _page.SelectedProject!.Journeys[1];
        ClearInvocations();
        old.Name = "Old journey";
        station.Name = "Old station";
        old.RefreshStations();
        Assert.That(_page.SelectedJourney, Is.SameAs(_session.SelectedJourney));
        Assert.That(_page.SelectedStation, Is.Null);
        Assert.That(_page.JourneysPageSelectedObject, Is.Null);
        VerifyNoSavesOrRuntimeUpdates();
        _page.SelectedJourney!.Name = "Current journey";
        Assert.That(SavedProject().Journeys[1].Name, Is.EqualTo("Current journey"));
    }

    [Test]
    public void ProjectSwitchPublishesTheNewItemsSourceBeforeItsJourneySelection()
    {
        // ListView ignores a SelectedItem outside its current Items collection.
        // https://learn.microsoft.com/en-us/windows/apps/develop/ui/controls/listview-and-gridview#single-selection
        var boundJourneys = _page.SelectedProject!.Journeys;
        var selectionsOutsideItemsSource = new List<JourneyViewModel>();
        _page.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(JourneysPageViewModel.SelectedProject))
                boundJourneys = _page.SelectedProject!.Journeys;
            if (args.PropertyName == nameof(JourneysPageViewModel.SelectedJourney)
                && _page.SelectedJourney is { } journey && !boundJourneys.Contains(journey))
                selectionsOutsideItemsSource.Add(journey);
        };

        var next = _session.AddProject(new Project { Journeys = [new Journey { Name = "Next" }] });

        using var assertions = Assert.EnterMultipleScope();
        Assert.That(selectionsOutsideItemsSource, Is.Empty,
            "The ListView cannot select a journey before its ItemsSource contains it.");
        Assert.That(boundJourneys, Is.SameAs(next.Journeys));
        Assert.That(_session.SelectedJourney, Is.SameAs(next.Journeys[0]));
    }

    [Test]
    public void ProjectSwitchSelectsFirstJourneyAndClearsOldPageSubscriptions()
    {
        using var assertions = Assert.EnterMultipleScope();
        var oldProject = _page.SelectedProject!;
        var oldJourney = _page.SelectedJourney!;
        var station = oldJourney.Stations[0];
        _page.SelectedStation = station;
        var next = _session.AddProject(new Project { Journeys = [new Journey { Name = "Next" }] });
        Assert.That(_page.SelectedProject, Is.SameAs(next));
        Assert.That(_page.SelectedJourney, Is.SameAs(next.Journeys[0]));
        ClearInvocations();
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        oldProject.Journeys.Clear();
        oldJourney.RefreshStations();
        oldJourney.Name = "Stale";
        station.Name = "Stale station";
        Assert.That(notifications, Is.Empty);
        Assert.That(_page.SelectedStation, Is.Null);
        VerifyNoSavesOrRuntimeUpdates();
        _session.SelectedProject = null;
        Assert.That(_session.SelectedJourney, Is.Null);
        Assert.That(_page.AddStationCommand.CanExecute(null), Is.False);
        Assert.That(_page.AddStationFromCityCommand.CanExecute(null), Is.False);
        Assert.That(_page.ResetJourneyCommand.CanExecute(null), Is.False);
    }

    [Test]
    public async Task SolutionReplacementClearsPageSelectionAndDoesNotSaveOldWrappers()
    {
        using var assertions = Assert.EnterMultipleScope();
        var journey = _page.SelectedJourney!;
        var station = journey.Stations[0];
        _page.SelectedStation = station;
        _page.JourneysPageItemClickedCommand.Execute(station);
        await _session.NewSolutionAsync().ConfigureAwait(false);
        ClearInvocations();
        station.Name = "Old station";
        journey.Name = "Old journey";
        Assert.That(_page.SelectedProject, Is.SameAs(_session.SelectedProject));
        Assert.That(_page.SelectedJourney, Is.Null);
        Assert.That(_page.SelectedStation, Is.Null);
        Assert.That(_page.JourneysPageSelectedObject, Is.Null);
        VerifyNoSavesOrRuntimeUpdates();
    }

    [Test]
    public void AutoSaveSuppressionPreservesEditsWithoutSavingOrRefreshingRuntime()
    {
        _page.SelectedStation = _page.SelectedJourney!.Stations[0];
        using (_session.SuppressAutoSave())
        {
            _page.SelectedStation.Name = "Suppressed station";
            _page.SelectedJourney.Name = "Suppressed journey";
            _page.AddStationCommand.Execute(null);
        }
        VerifyNoSavesOrRuntimeUpdates();
        Assert.That(_page.SelectedJourney.Stations, Has.Count.EqualTo(2));
    }

    [Test]
    public void StationReorderingPreservesIdentityOrderAndPersistence()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.AddStationCommand.Execute(null);
        var first = _page.SelectedJourney!.Stations[0];
        var last = _page.SelectedStation!;
        _page.SelectedJourney.MoveStationTo(last, 0);
        Assert.That(_page.SelectedJourney.Stations[0], Is.SameAs(last));
        Assert.That(_page.SelectedJourney.Stations[1], Is.SameAs(first));
        Assert.That(SavedProject().Journeys[0].Stations[0].Id, Is.EqualTo(last.Model.Id));
    }

    [Test]
    public async Task ResetUsesInjectedGatewayAndTheCurrentSharedSelectionOnly()
    {
        var first = _page.SelectedJourney!;
        await _page.ResetJourneyCommand.ExecuteAsync(null).ConfigureAwait(false);
        var next = _session.AddProject(new Project { Journeys = [new Journey()] }).Journeys[0];
        await _page.ResetJourneyCommand.ExecuteAsync(null).ConfigureAwait(false);
        _session.SelectedJourney = null;
        await _page.ResetJourneyCommand.ExecuteAsync(null).ConfigureAwait(false);
        _gateway.Verify(value => value.ResetJourneyAsync(first.Id, It.IsAny<CancellationToken>()), Times.Once);
        _gateway.Verify(value => value.ResetJourneyAsync(next.Id, It.IsAny<CancellationToken>()), Times.Once);
        _runtime.Verify(value => value.ResetJourneyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CityLibraryLoadsThroughUiDispatcherAndFiltersThroughExistingService()
    {
        var city = new City { Name = "Berlin" };
        _cities.Setup(value => value.LoadCitiesAsync()).ReturnsAsync([city]);
        _cities.Setup(value => value.FilterCities("ber")).Returns([city]);
        Action? update = null;
        _dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => update = action);
        await _page.LoadCityLibraryAsync().ConfigureAwait(false);
        Assert.That(_page.CityLibrary, Is.Empty);
        update!();
        Assert.That(_page.CityLibrary.Single(), Is.SameAs(city));
        _page.CitySearchText = "ber";
        _cities.Verify(value => value.FilterCities("ber"), Times.Once);
        Assert.That(_page.CityLibrary.Single(), Is.SameAs(city));
    }

    [Test]
    public async Task DisposeDropsPendingCityLoadAndDetachesPageSubscriptionsWithoutUntrackingSessionJourney()
    {
        var pending = new TaskCompletionSource<List<City>>(TaskCreationOptions.RunContinuationsAsynchronously);
        _cities.Setup(value => value.LoadCitiesAsync()).Returns(pending.Task);
        var load = _page.LoadCityLibraryAsync();
        var journey = _page.SelectedJourney!;
        var station = journey.Stations[0];
        _page.SelectedStation = station;
        _page.Dispose();
        _page.Dispose();
        pending.SetResult([new City()]);
        await load.ConfigureAwait(false);
        Assert.That(_page.CityLibrary, Is.Empty);
        station.Name = "Detached";
        VerifyNoSavesOrRuntimeUpdates();
        journey.Name = "Still tracked by session";
        Assert.That(SavedProject().Journeys[0].Name, Is.EqualTo("Still tracked by session"));
        var notifications = new List<string?>();
        _page.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        _session.SelectedJourney = _page.SelectedProject!.Journeys[1];
        _session.SelectedProject = null;
        Assert.That(notifications, Is.Empty);
    }

    [Test]
    public void LayoutRestoresExistingFlagsAndPersistsOnlyChanges()
    {
        using var assertions = Assert.EnterMultipleScope();
        _page.Dispose();
        var layout = _settings.Layout.JourneysPage;
        layout.IsJourneyListExpanded = false;
        layout.IsStationListExpanded = false;
        layout.IsCityLibraryExpanded = false;
        layout.IsJourneyPropertiesExpanded = false;
        _page = CreatePage();
        _page.IsJourneyListExpanded = false;
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Never);
        Assert.That(_page.IsStationListExpanded, Is.False);
        Assert.That(_page.IsCityLibraryVisible, Is.False);
        Assert.That(_page.IsJourneyPropertiesExpanded, Is.False);
        _page.IsJourneyListExpanded = true;
        _page.IsStationListExpanded = true;
        _page.IsCityLibraryVisible = true;
        _page.IsJourneyPropertiesExpanded = true;
        Assert.That(layout.IsJourneyListExpanded && layout.IsStationListExpanded
            && layout.IsCityLibraryExpanded && layout.IsJourneyPropertiesExpanded, Is.True);
        _persistence.Verify(value => value.SaveSettingsAsync(_settings), Times.Exactly(4));
    }

    [Test]
    public void EventEditorSharesSelectionAndReceivesStatusChangesUntilDisposed()
    {
        using var assertions = Assert.EnterMultipleScope();
        using var library = new WorkflowLibraryViewModel(_session);
        using var editor = new EventManagerViewModel(_session, library, _page,
            new CommunityToolkit.Mvvm.Input.RelayCommand(() => { }));
        var notifications = new List<string?>();
        editor.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);
        _page.JourneyCommandStatus = "Reset complete";
        Assert.That(editor.HasCommandStatus, Is.True);
        Assert.That(notifications, Does.Contain(nameof(EventManagerViewModel.HasCommandStatus)));
        editor.SelectedJourney = _page.SelectedProject!.Journeys[1];
        Assert.That(_page.SelectedJourney, Is.SameAs(editor.SelectedJourney));
        editor.Dispose();
        notifications.Clear();
        _page.JourneyCommandStatus = string.Empty;
        Assert.That(notifications, Is.Empty);
    }

    private Project SavedProject() => JsonSerializer.Deserialize<Solution>(_lastSaved)!.Projects[0];

    private void ClearInvocations()
    {
        _io.Invocations.Clear();
        _runtime.Invocations.Clear();
    }

    private void VerifyNoSavesOrRuntimeUpdates()
    {
        _io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        _runtime.Verify(value => value.UpdateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
