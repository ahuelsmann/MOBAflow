// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI.View;

using Converters;

using SharedUI.Interface;
using SharedUI.ViewModel;

public partial class EnginePage
{
    private readonly MauiViewModel _mauiViewModel;
    private readonly TrainControlViewModel _viewModel;
    private Task? _runtimeInitializationTask;

    public event EventHandler? NavigateToControlTabRequested;

    public EnginePage(
        TrainControlViewModel viewModel,
        MauiViewModel mauiViewModel,
        IPhotoUriResolver photoUriResolver)
    {
        _mauiViewModel = mauiViewModel;
        _viewModel = viewModel;

        Resources.Add("RemotePhotoSourceConverter", new RemotePhotoSourceConverter(photoUriResolver));
        Resources["SelectLocomotiveCommand"] = new Command<LocomotiveViewModel?>(OnSelectLocomotive);

        BindingContext = viewModel;
        InitializeComponent();
        ConnectionHeader.BindingContext = mauiViewModel;
    }

    public void ActivateTab()
    {
        Dispatcher.DispatchAsync(async () =>
        {
            _runtimeInitializationTask ??= _mauiViewModel.InitializeAsync();
            await _runtimeInitializationTask.ConfigureAwait(false);
        });
    }

    public void DeactivateTab()
    {
    }

    private void OnSelectLocomotive(LocomotiveViewModel? locomotive)
    {
        if (locomotive == null)
        {
            return;
        }

        _viewModel.SelectProjectLocomotiveCommand.Execute(locomotive);
        NavigateToControlTabRequested?.Invoke(this, EventArgs.Empty);
    }
}
