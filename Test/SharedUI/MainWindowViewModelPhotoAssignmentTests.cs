// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Test.Helpers;
using Moba.Domain.Enum;

using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Contracts.Runtime;
using Moba.Domain;
using Moba.SharedUI.Interface;
using Moba.SharedUI.Service;
using Moba.SharedUI.ViewModel;

using Moq;
using CommunityToolkit.Mvvm.Input;

[TestFixture]
internal sealed partial class MainWindowViewModelPhotoAssignmentTests : IDisposable
{
    private readonly TestRollingStockPages _rollingStockPages = new();

    [TearDown]
    public void Dispose()
    {
        _rollingStockPages.Dispose();
        GC.SuppressFinalize(this);
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedLocomotive()
    {
        var viewModel = CreateViewModel();
        var locomotive = new LocomotiveViewModel(new Locomotive { Name = "BR 218" });
        viewModel.UpdateActivePhotoAssignmentPageTag("locomotives");
        viewModel.LocomotivesPage.SelectedLocomotive = locomotive;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/a.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.Locomotive));
        Assert.That(locomotive.PhotoPath, Is.EqualTo("photos/temp/a.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedPassengerWagon()
    {
        var viewModel = CreateViewModel();
        var passengerWagon = new PassengerWagonViewModel(new PassengerWagon { Name = "Avmz" });
        viewModel.UpdateActivePhotoAssignmentPageTag("passengerwagons");
        viewModel.WagonsPage.SelectedPassengerWagon = passengerWagon;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/b.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.PassengerWagon));
        Assert.That(passengerWagon.PhotoPath, Is.EqualTo("photos/temp/b.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_AssignsToSelectedGoodsWagon()
    {
        var viewModel = CreateViewModel();
        var goodsWagon = new GoodsWagonViewModel(new GoodsWagon { Name = "Eaos" });
        viewModel.UpdateActivePhotoAssignmentPageTag("goodswagons");
        viewModel.WagonsPage.SelectedGoodsWagon = goodsWagon;

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/c.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.GoodsWagon));
        Assert.That(goodsWagon.PhotoPath, Is.EqualTo("photos/temp/c.jpg"));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_ReturnsNoneWhenNothingSelected()
    {
        var viewModel = CreateViewModel();
        viewModel.UpdateActivePhotoAssignmentPageTag("locomotives");

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/d.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.None));
    }

    [Test]
    public void AssignUploadedPhotoToSelectedEntity_UsesCurrentPageAndIgnoresStaleOtherSelection()
    {
        var viewModel = CreateViewModel();
        var locomotive = new LocomotiveViewModel(new Locomotive { Name = "BR 218" });
        var passengerWagon = new PassengerWagonViewModel(new PassengerWagon { Name = "Avmz" });
        viewModel.LocomotivesPage.SelectedLocomotive = locomotive;
        viewModel.WagonsPage.SelectedPassengerWagon = passengerWagon;
        viewModel.UpdateActivePhotoAssignmentPageTag("passengerwagons");

        var result = viewModel.AssignUploadedPhotoToSelectedEntity("photos/temp/e.jpg");

        Assert.That(result, Is.EqualTo(PhotoAssignmentTarget.PassengerWagon));
        Assert.That(passengerWagon.PhotoPath, Is.EqualTo("photos/temp/e.jpg"));
        Assert.That(locomotive.PhotoPath, Is.Null);
    }

    private MainWindowViewModel CreateViewModel()
    {
        var mobaRuntimeMock = new Mock<IMobaRuntime>();
        mobaRuntimeMock.SetupGet(client => client.Current).Returns(MobaRuntimeSnapshot.Empty);
        mobaRuntimeMock.Setup(client => client.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        mobaRuntimeMock.Setup(client => client.DisconnectAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var eventBusMock = new Mock<IEventBus>();
        var uiDispatcherMock = new Mock<IUiDispatcher>();
        var loggerMock = new Mock<ILogger<MainWindowViewModel>>();

        uiDispatcherMock
            .Setup(dispatcher => dispatcher.InvokeOnUi(It.IsAny<Action>()))
            .Callback<Action>(action => action());

        var locomotiveSession1 = TestSolutionSessions.Create(new Solution(), uiDispatcherMock.Object, mobaRuntimeMock.Object);
        var stationEventBus1 = eventBusMock.Object;
        return new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            mobaRuntimeMock.Object,
            mobaRuntimeMock.Object,
            mobaRuntimeMock.Object,
            new LocalRuntimeCommandGateway(mobaRuntimeMock.Object),
            stationEventBus1,
            TestSettingsPages.Create(new AppSettings(), uiDispatcherMock.Object),
            _rollingStockPages.GetTrainsPage(locomotiveSession1, mobaRuntimeMock.Object, null),
            _rollingStockPages.GetStationsPage(locomotiveSession1, mobaRuntimeMock.Object, new ActionExecutionContext
            {
                Z21 = new Mock<IZ21>().Object
            }, stationEventBus1),
            _rollingStockPages.GetJourneysPage(locomotiveSession1, new LocalRuntimeCommandGateway(mobaRuntimeMock.Object), uiDispatcherMock.Object),
            locomotiveSession1,
            loggerMock.Object);
    }

    private static readonly string[] RenamedVehicleNames = ["Renamed vehicle"];

    [TestCase(TrainVehicleKind.Locomotive)]
    [TestCase(TrainVehicleKind.PassengerWagon)]
    [TestCase(TrainVehicleKind.GoodsWagon)]
    public void InventoryCommands_RefreshFilteredListsAndSaveFinalModel(TrainVehicleKind kind)
    {
        // Arrange: use the same property notifications as the three vehicle-page bindings.
        var project = new Project();
        var savedJson = string.Empty;
        var viewModel = CreateViewModel(project, json => savedJson = json);
        var observedNames = Array.Empty<string>();
        NotificationSource(viewModel, kind).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == FilterProperty(kind))
                observedNames = Names(viewModel, kind);
        };
        SetSearch(viewModel, kind, " 1 ");

        // Act/Assert: adding under a search updates the list before selecting the new item.
        AddCommand(viewModel, kind).Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedNames, Has.Length.EqualTo(1));
            Assert.That(Selection(viewModel, kind), Is.Not.Null);
            Assert.That(ModelCount(JsonSerializer.Deserialize<Solution>(savedJson, JsonOptions.Default)!.Projects[0], kind), Is.EqualTo(1));
        }

        RenameSelected(viewModel, kind, "Renamed vehicle");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedNames, Is.Empty);
            Assert.That(savedJson, Does.Contain("Renamed vehicle"));
        }

        SetSearch(viewModel, kind, "RENAMED");
        Assert.That(observedNames, Is.EqualTo(RenamedVehicleNames));

        // Removing a row clears selection synchronously. The command must still remove its model.
        DeleteCommand(viewModel, kind).Execute(null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedNames, Is.Empty);
            Assert.That(Selection(viewModel, kind), Is.Null);
            Assert.That(ModelCount(project, kind), Is.Zero);
            Assert.That(ModelCount(JsonSerializer.Deserialize<Solution>(savedJson, JsonOptions.Default)!.Projects[0], kind), Is.Zero);
        }
    }

    [TestCase(TrainVehicleKind.Locomotive)]
    [TestCase(TrainVehicleKind.PassengerWagon)]
    [TestCase(TrainVehicleKind.GoodsWagon)]
    public void ProjectChange_RefreshesListsClearsSelectionAndDetachesOldInventory(TrainVehicleKind kind)
    {
        var viewModel = CreateViewModel(new Project(), _ => { });
        AddCommand(viewModel, kind).Execute(null);
        var oldProject = viewModel.SelectedProject!;
        var oldSelection = Selection(viewModel, kind);
        var notifications = 0;
        var observedNames = Names(viewModel, kind);
        NotificationSource(viewModel, kind).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == FilterProperty(kind))
            {
                notifications++;
                observedNames = Names(viewModel, kind);
            }
        };

        viewModel.SelectedProject = new ProjectViewModel(new Project());
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedNames, Is.Empty);
            Assert.That(Selection(viewModel, kind), Is.Null);
            Assert.That(notifications, Is.GreaterThan(0));
        }

        notifications = 0;
        oldProject.Locomotives.Clear();
        oldProject.PassengerWagons.Clear();
        oldProject.GoodsWagons.Clear();
        if (oldSelection is LocomotiveViewModel locomotive)
            locomotive.Name = "Detached";
        else if (oldSelection is WagonViewModel wagon)
            wagon.Name = "Detached";
        Assert.That(notifications, Is.Zero);

        AddCommand(viewModel, kind).Execute(null);
        Assert.That(observedNames, Has.Length.EqualTo(1));
        viewModel.SelectedProject = null;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(observedNames, Is.Empty);
            Assert.That(Selection(viewModel, kind), Is.Null);
        }
    }

    [TestCase(TrainVehicleKind.Locomotive, "")]
    [TestCase(TrainVehicleKind.PassengerWagon, "")]
    [TestCase(TrainVehicleKind.GoodsWagon, "")]
    [TestCase(TrainVehicleKind.Locomotive, "1")]
    [TestCase(TrainVehicleKind.PassengerWagon, "1")]
    [TestCase(TrainVehicleKind.GoodsWagon, "1")]
    public void RenameSelected_PreservesBindingSourceAndSelection(TrainVehicleKind kind, string search)
    {
        var viewModel = CreateViewModel(new Project(), _ => { });
        AddCommand(viewModel, kind).Execute(null);
        SetSearch(viewModel, kind, search);
        var selected = Selection(viewModel, kind);
        var source = BindingSource(viewModel, kind);
        NotificationSource(viewModel, kind).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == FilterProperty(kind) && !ReferenceEquals(source, BindingSource(viewModel, kind)))
            {
                // WinUI Selector clears its two-way selection when ItemsSource is replaced.
                switch (kind)
                {
                    case TrainVehicleKind.Locomotive: viewModel.LocomotivesPage.SelectedLocomotive = null; break;
                    case TrainVehicleKind.PassengerWagon: viewModel.WagonsPage.SelectedPassengerWagon = null; break;
                    default: viewModel.WagonsPage.SelectedGoodsWagon = null; break;
                }
            }
        };

        RenameSelected(viewModel, kind, "Renamed vehicle 1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(BindingSource(viewModel, kind), Is.SameAs(source));
            Assert.That(Selection(viewModel, kind), Is.SameAs(selected));
        }
    }

    private static System.ComponentModel.INotifyPropertyChanged NotificationSource(MainWindowViewModel viewModel, TrainVehicleKind kind)
        => kind == TrainVehicleKind.Locomotive ? viewModel.LocomotivesPage : viewModel.WagonsPage;

    private static object BindingSource(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.LocomotivesPage.FilteredLocomotiveLibrary,
        TrainVehicleKind.PassengerWagon => viewModel.WagonsPage.FilteredPassengerWagonLibrary,
        _ => viewModel.WagonsPage.FilteredGoodsWagonLibrary
    };

    [TestCase(TrainVehicleKind.Locomotive, "selection")]
    [TestCase(TrainVehicleKind.PassengerWagon, "selection")]
    [TestCase(TrainVehicleKind.GoodsWagon, "selection")]
    [TestCase(TrainVehicleKind.Locomotive, "project")]
    [TestCase(TrainVehicleKind.PassengerWagon, "project")]
    [TestCase(TrainVehicleKind.GoodsWagon, "project")]
    [TestCase(TrainVehicleKind.Locomotive, "solution")]
    [TestCase(TrainVehicleKind.PassengerWagon, "solution")]
    [TestCase(TrainVehicleKind.GoodsWagon, "solution")]
    [TestCase(TrainVehicleKind.Locomotive, "failure")]
    [TestCase(TrainVehicleKind.PassengerWagon, "failure")]
    [TestCase(TrainVehicleKind.GoodsWagon, "failure")]
    public async Task BrowsePhoto_WhenContextChangesDuringCopy_PersistsOnlyInOriginalSolution(
        TrainVehicleKind kind, string change)
    {
        var project = new Project();
        var savedJson = string.Empty;
        var copy = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var io = TestFileServices.CreateMock();
        io.As<IFilePickerService>().Setup(value => value.BrowseForPhotoAsync()).ReturnsAsync("source.png");
        io.As<IPhotoStorageService>().Setup(value => value.SavePhotoAsync("source.png", It.IsAny<string>(), It.IsAny<Guid>()))
            .Returns(copy.Task);
        var viewModel = CreateViewModel(project, json => savedJson = json, io);
        AddCommand(viewModel, kind).Execute(null);
        var original = Selection(viewModel, kind);
        var browse = original is LocomotiveViewModel locomotive
            ? locomotive.BrowsePhotoCommand
            : ((WagonViewModel)original!).BrowsePhotoCommand;
        var pending = ((IAsyncRelayCommand)browse!).ExecuteAsync(null);
        Assert.That(pending.IsCompleted, Is.False);

        if (change is "project" or "solution")
        {
            var nextProject = new Project();
            if (change == "solution")
            {
                // Loading replaces projects in the existing singleton Solution instance.
                viewModel.Solution.Projects.Clear();
                viewModel.CurrentSolutionPath = "other.json";
            }
            viewModel.Solution.Projects.Add(nextProject);
            viewModel.SelectedProject = new ProjectViewModel(nextProject);
        }
        AddCommand(viewModel, kind).Execute(null);
        savedJson = string.Empty;
        io.Invocations.Clear();
        if (change == "failure")
        {
            io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
                .ThrowsAsync(new IOException("Photo persistence test failure"));
        }

        copy.SetResult("imported.png");
        await pending.ConfigureAwait(false);

        var photoPath = original is LocomotiveViewModel originalLocomotive
            ? originalLocomotive.PhotoPath
            : ((WagonViewModel)original!).PhotoPath;
        if (change == "solution")
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(photoPath, Is.Null);
                Assert.That(savedJson, Is.Empty);
            }
            io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()), Times.Never);
        }
        else if (change == "failure")
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(photoPath, Is.EqualTo("imported.png"));
                Assert.That(savedJson, Is.Empty);
                Assert.That(viewModel.HasUnsavedChanges, Is.True);
                Assert.That(viewModel.SolutionSaveState, Is.EqualTo(SolutionSaveState.NotSaved));
                Assert.That(viewModel.SolutionSaveStatusText, Does.Contain("Photo persistence test failure"));
            }
        }
        else
        {
            var restored = JsonSerializer.Deserialize<Solution>(savedJson, JsonOptions.Default)!;
            var restoredProject = restored.Projects.Single(value => value.Id == project.Id);
            var restoredPath = kind switch
            {
                TrainVehicleKind.Locomotive => restoredProject.Locomotives[0].PhotoPath,
                TrainVehicleKind.PassengerWagon => restoredProject.PassengerWagons[0].PhotoPath,
                _ => restoredProject.GoodsWagons[0].PhotoPath
            };
            using (Assert.EnterMultipleScope())
            {
                Assert.That(photoPath, Is.EqualTo("imported.png"));
                Assert.That(restoredPath, Is.EqualTo("imported.png"));
            }
            io.Verify(value => value.SaveAsync(It.IsAny<Solution>(), "inventory.json"), Times.Once);
        }
    }

    private MainWindowViewModel CreateViewModel(Project project, Action<string> save, Mock<ISolutionIoService>? io = null)
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        runtime.Setup(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        io ??= TestFileServices.CreateMock();
        io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .Callback((Solution solution, string _) => save(JsonSerializer.Serialize(solution, JsonOptions.Default)))
            .ReturnsAsync((Solution _, string path) => (true, (string?)path, (string?)null));
        var locomotiveSession2 = TestSolutionSessions.Create(new Solution { Projects = [project] }, dispatcher.Object, runtime.Object, io.Object);
        var stationEventBus2 = new Mock<IEventBus>().Object;
        var viewModel = new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(),
            runtime.Object,
            runtime.Object,
            runtime.Object,
            new LocalRuntimeCommandGateway(runtime.Object),
            stationEventBus2,
            TestSettingsPages.Create(new AppSettings(), dispatcher.Object),
            _rollingStockPages.GetTrainsPage(locomotiveSession2, runtime.Object, io.Object),
            _rollingStockPages.GetStationsPage(locomotiveSession2, runtime.Object, new ActionExecutionContext { Z21 = new Mock<IZ21>().Object }, stationEventBus2),
            _rollingStockPages.GetJourneysPage(locomotiveSession2, new LocalRuntimeCommandGateway(runtime.Object), dispatcher.Object),
            locomotiveSession2,
            NullLogger<MainWindowViewModel>.Instance)
        {
            CurrentSolutionPath = "inventory.json",
            SelectedProject = new ProjectViewModel(project)
        };
        return viewModel;
    }

    private static string FilterProperty(TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => nameof(LocomotivesPageViewModel.FilteredLocomotiveLibrary),
        TrainVehicleKind.PassengerWagon => nameof(WagonsPageViewModel.FilteredPassengerWagonLibrary),
        _ => nameof(WagonsPageViewModel.FilteredGoodsWagonLibrary)
    };

    private static string[] Names(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.LocomotivesPage.FilteredLocomotiveLibrary.Select(item => item.Name).ToArray(),
        TrainVehicleKind.PassengerWagon => viewModel.WagonsPage.FilteredPassengerWagonLibrary.Select(item => item.Name).ToArray(),
        _ => viewModel.WagonsPage.FilteredGoodsWagonLibrary.Select(item => item.Name).ToArray()
    };

    private static object? Selection(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.LocomotivesPage.SelectedLocomotive,
        TrainVehicleKind.PassengerWagon => viewModel.WagonsPage.SelectedPassengerWagon,
        _ => viewModel.WagonsPage.SelectedGoodsWagon
    };

    private static int ModelCount(Project project, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => project.Locomotives.Count,
        TrainVehicleKind.PassengerWagon => project.PassengerWagons.Count,
        _ => project.GoodsWagons.Count
    };

    private static ICommand AddCommand(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.LocomotivesPage.AddLocomotiveCommand,
        TrainVehicleKind.PassengerWagon => viewModel.WagonsPage.AddPassengerWagonCommand,
        _ => viewModel.WagonsPage.AddGoodsWagonCommand
    };

    private static ICommand DeleteCommand(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.LocomotivesPage.DeleteLocomotiveCommand,
        TrainVehicleKind.PassengerWagon => viewModel.WagonsPage.DeletePassengerWagonCommand,
        _ => viewModel.WagonsPage.DeleteGoodsWagonCommand
    };

    private static void SetSearch(MainWindowViewModel viewModel, TrainVehicleKind kind, string search)
    {
        switch (kind)
        {
            case TrainVehicleKind.Locomotive: viewModel.LocomotivesPage.LocomotiveSearchText = search; break;
            case TrainVehicleKind.PassengerWagon: viewModel.WagonsPage.PassengerWagonSearchText = search; break;
            default: viewModel.WagonsPage.GoodsWagonSearchText = search; break;
        }
    }

    private static void RenameSelected(MainWindowViewModel viewModel, TrainVehicleKind kind, string name)
    {
        switch (kind)
        {
            case TrainVehicleKind.Locomotive: viewModel.LocomotivesPage.SelectedLocomotive!.Name = name; break;
            case TrainVehicleKind.PassengerWagon: viewModel.WagonsPage.SelectedPassengerWagon!.Name = name; break;
            default: viewModel.WagonsPage.SelectedGoodsWagon!.Name = name; break;
        }
    }
}
