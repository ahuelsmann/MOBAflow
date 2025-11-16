namespace Moba.Smart;

using SharedUI.ViewModel;

public partial class MainPage : ContentPage
{
	private readonly CounterViewModel _viewModel;

	public MainPage(CounterViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;

		// Subscribe to TargetReached event for showing alert
		_viewModel.TargetReached += OnTargetReached;
	}

	private void OnTargetReached(object? sender, InPortStatistic stat)
	{
		// Show alert on UI thread (using async version)
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			await DisplayAlertAsync(
				"🎉 Target Reached!",
				$"Track {stat.InPort} has completed {stat.Count} laps!\n\nLast lap: {stat.LastLapTimeFormatted}",
				"OK"
			);
		});
	}
}
