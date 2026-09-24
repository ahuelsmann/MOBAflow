// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Test.SharedUI;

using System.Text.Json;
using System.Windows.Input;
using Microsoft.Extensions.Logging.Abstractions;
using Moba.Backend.Interface;
using Moba.Backend.Model;
using Moba.Backend.Service;
using Moba.Common.Configuration;
using Moba.Common.Events;
using Moba.Common.Runtime;
using Moba.Domain;
using Moba.Domain.Enum;
using Moba.SharedUI.Interface;
using Moba.SharedUI.ViewModel;
using Moq;

internal sealed class RollingStockInventoryTests
{
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
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == FilterProperty(kind))
                observedNames = Names(viewModel, kind);
        };
        SetSearch(viewModel, kind, "1");

        // Act/Assert: adding under a search updates the list before selecting the new item.
        AddCommand(viewModel, kind).Execute(null);
        Assert.That(observedNames, Has.Length.EqualTo(1));
        Assert.That(Selection(viewModel, kind), Is.Not.Null);
        Assert.That(ModelCount(JsonSerializer.Deserialize<Solution>(savedJson, JsonOptions.Default)!.Projects[0], kind), Is.EqualTo(1));

        RenameSelected(viewModel, kind, "Renamed vehicle");
        Assert.That(observedNames, Is.Empty);
        Assert.That(savedJson, Does.Contain("Renamed vehicle"));

        SetSearch(viewModel, kind, "RENAMED");
        Assert.That(observedNames, Is.EqualTo(new[] { "Renamed vehicle" }));

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
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == FilterProperty(kind))
            {
                notifications++;
                observedNames = Names(viewModel, kind);
            }
        };

        viewModel.SelectedProject = new ProjectViewModel(new Project());
        Assert.That(observedNames, Is.Empty);
        Assert.That(Selection(viewModel, kind), Is.Null);
        Assert.That(notifications, Is.GreaterThan(0));

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
        Assert.That(observedNames, Is.Empty);
        Assert.That(Selection(viewModel, kind), Is.Null);
    }

    private static MainWindowViewModel CreateViewModel(Project project, Action<string> save)
    {
        var runtime = new Mock<IMobaRuntime>();
        runtime.SetupGet(value => value.Current).Returns(MobaRuntimeSnapshot.Empty);
        runtime.Setup(value => value.GetTrafficPackets()).Returns(Array.Empty<Z21TrafficPacket>());
        runtime.Setup(value => value.ActivateProjectAsync(It.IsAny<Project>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var dispatcher = new Mock<IUiDispatcher>();
        dispatcher.Setup(value => value.InvokeOnUi(It.IsAny<Action>())).Callback<Action>(action => action());
        var io = new Mock<IIoService>();
        io.Setup(value => value.SaveAsync(It.IsAny<Solution>(), It.IsAny<string>()))
            .Returns((Solution solution, string path) =>
            {
                save(JsonSerializer.Serialize(solution, JsonOptions.Default));
                return Task.FromResult((true, (string?)path, (string?)null));
            });
        var viewModel = new MainWindowViewModel(
            new LayoutColumnWidthsViewModel(), runtime.Object, new Mock<IEventBus>().Object,
            dispatcher.Object, new AppSettings(), new Solution { Projects = [project] },
            new ActionExecutionContext { Z21 = new Mock<IZ21>().Object },
            NullLogger<MainWindowViewModel>.Instance, io.Object)
        {
            CurrentSolutionPath = "inventory.json",
            SelectedProject = new ProjectViewModel(project)
        };
        return viewModel;
    }

    private static string FilterProperty(TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => nameof(MainWindowViewModel.FilteredLocomotiveLibrary),
        TrainVehicleKind.PassengerWagon => nameof(MainWindowViewModel.FilteredPassengerWagonLibrary),
        _ => nameof(MainWindowViewModel.FilteredGoodsWagonLibrary)
    };

    private static string[] Names(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.FilteredLocomotiveLibrary.Select(item => item.Name).ToArray(),
        TrainVehicleKind.PassengerWagon => viewModel.FilteredPassengerWagonLibrary.Select(item => item.Name).ToArray(),
        _ => viewModel.FilteredGoodsWagonLibrary.Select(item => item.Name).ToArray()
    };

    private static object? Selection(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.SelectedLocomotive,
        TrainVehicleKind.PassengerWagon => viewModel.SelectedPassengerWagon,
        _ => viewModel.SelectedGoodsWagon
    };

    private static int ModelCount(Project project, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => project.Locomotives.Count,
        TrainVehicleKind.PassengerWagon => project.PassengerWagons.Count,
        _ => project.GoodsWagons.Count
    };

    private static ICommand AddCommand(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.AddLocomotiveCommand,
        TrainVehicleKind.PassengerWagon => viewModel.AddPassengerWagonCommand,
        _ => viewModel.AddGoodsWagonCommand
    };

    private static ICommand DeleteCommand(MainWindowViewModel viewModel, TrainVehicleKind kind) => kind switch
    {
        TrainVehicleKind.Locomotive => viewModel.DeleteLocomotiveCommand,
        TrainVehicleKind.PassengerWagon => viewModel.DeletePassengerWagonCommand,
        _ => viewModel.DeleteGoodsWagonCommand
    };

    private static void SetSearch(MainWindowViewModel viewModel, TrainVehicleKind kind, string search)
    {
        switch (kind)
        {
            case TrainVehicleKind.Locomotive: viewModel.LocomotiveSearchText = search; break;
            case TrainVehicleKind.PassengerWagon: viewModel.PassengerWagonSearchText = search; break;
            default: viewModel.GoodsWagonSearchText = search; break;
        }
    }

    private static void RenameSelected(MainWindowViewModel viewModel, TrainVehicleKind kind, string name)
    {
        switch (kind)
        {
            case TrainVehicleKind.Locomotive: viewModel.SelectedLocomotive!.Name = name; break;
            case TrainVehicleKind.PassengerWagon: viewModel.SelectedPassengerWagon!.Name = name; break;
            default: viewModel.SelectedGoodsWagon!.Name = name; break;
        }
    }
}
