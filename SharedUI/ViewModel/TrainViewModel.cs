namespace Moba.SharedUI.ViewModel;

using Backend.Model;
using Backend.Model.Enum;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Moba.SharedUI.Service;

using System.Collections.ObjectModel;

/// <summary>
/// ViewModel wrapper for Train model with CRUD operations for Locomotives and Wagons.
/// </summary>
public partial class TrainViewModel : ObservableObject
{
    [ObservableProperty]
    private Train model;

    private readonly IUiDispatcher? _dispatcher;

    public TrainViewModel(Train model, IUiDispatcher? dispatcher = null)
    {
        Model = model;
        _dispatcher = dispatcher;

        // Wrap existing locomotives and wagons in ViewModels
        Locomotives = new ObservableCollection<LocomotiveViewModel>(
            model.Locomotives.Select(l => new LocomotiveViewModel(l, dispatcher))
        );

        Wagons = new ObservableCollection<WagonViewModel>(
            model.Wagons.Select(w => new WagonViewModel(w, dispatcher))
        );
    }

    public bool IsDoubleTraction
    {
        get => Model.IsDoubleTraction;
        set => SetProperty(Model.IsDoubleTraction, value, Model, (m, v) => m.IsDoubleTraction = v);
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public string Description
    {
        get => Model.Description ?? string.Empty;
        set => SetProperty(Model.Description, value, Model, (m, v) => m.Description = v);
    }

    public TrainType TrainType
    {
        get => Model.TrainType;
        set => SetProperty(Model.TrainType, value, Model, (m, v) => m.TrainType = v);
    }

    public ServiceType ServiceType
    {
        get => Model.ServiceType;
        set => SetProperty(Model.ServiceType, value, Model, (m, v) => m.ServiceType = v);
    }

    public ObservableCollection<LocomotiveViewModel> Locomotives { get; }

    public ObservableCollection<WagonViewModel> Wagons { get; }

    [RelayCommand]
    private void AddLocomotive()
    {
        var newLocomotive = new Locomotive { Name = "New Locomotive" };
        Model.Locomotives.Add(newLocomotive);

        var locomotiveVM = new LocomotiveViewModel(newLocomotive, _dispatcher);
        Locomotives.Add(locomotiveVM);
    }

    [RelayCommand]
    private void DeleteLocomotive(LocomotiveViewModel locomotiveVM)
    {
        if (locomotiveVM == null) return;

        Model.Locomotives.Remove(locomotiveVM.Model);
        Locomotives.Remove(locomotiveVM);
    }

    [RelayCommand]
    private void AddWagon()
    {
        var newWagon = new Wagon { Name = "New Wagon" };
        Model.Wagons.Add(newWagon);

        var wagonVM = new WagonViewModel(newWagon, _dispatcher);
        Wagons.Add(wagonVM);
    }

    [RelayCommand]
    private void DeleteWagon(WagonViewModel wagonVM)
    {
        if (wagonVM == null) return;

        Model.Wagons.Remove(wagonVM.Model);
        Wagons.Remove(wagonVM);
    }
}
