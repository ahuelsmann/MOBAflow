namespace Moba.SharedUI.ViewModel.Action;

using Backend.Model;
using Backend.Model.Action;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Diagnostics;

public partial class GongViewModel : ObservableObject
{
    [ObservableProperty]
    private Gong model;

    public GongViewModel(Gong model)
    {
        Model = model;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

    public async Task ExecuteAsync(Journey journey, Station station)
    {
        Debug.WriteLine("🔔 Gong wird abgespielt");

        await Task.CompletedTask;
    }

    //public Gong ToModel()
    //{
    //    return Model;
    //}

    //public static GongViewModel FromModel(Gong model)
    //{
    //    return new GongViewModel(model);
    //}
}