namespace Moba.SharedUI.ViewModel.Action;

using Backend.Model;
using Backend.Model.Action;

using CommunityToolkit.Mvvm.ComponentModel;

using System.Diagnostics;

public partial class AudioViewModel : ObservableObject
{
    [ObservableProperty]
    private Audio model;

    public AudioViewModel(Audio model)
    {
        Model = model;
    }

    public string Name
    {
        get => Model.Name;
        set => SetProperty(Model.Name, value, Model, (m, v) => m.Name = v);
    }

#pragma warning disable S2325 // Methods and properties that don't access instance data should be static
    public async Task ExecuteAsync(Journey journey, Station station)
    {
        Debug.WriteLine("🔔 Sound wird abgespielt");
        await Task.CompletedTask;
    }
#pragma warning restore S2325
}