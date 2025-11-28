// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Smart;

public partial class App : Application
{
	private readonly IServiceProvider _services;

	public App(IServiceProvider services)
	{
		_services = services;
		
		// ⚠️ CRITICAL: Load resources BEFORE InitializeComponent
		LoadEssentialResources();
		
		InitializeComponent();
	}

	private void LoadEssentialResources()
	{
		var resources = Resources ?? new ResourceDictionary();
		
		// Surface colors
		resources["SurfaceBackground"] = Color.FromArgb("#121212");
		resources["SurfaceCard"] = Color.FromArgb("#2D2D30");
		resources["SurfaceElevated"] = Color.FromArgb("#383838");
		resources["SurfaceHighlight"] = Color.FromArgb("#404040");
		resources["SurfaceDark"] = Color.FromArgb("#1E1E1E");
		
		// Railway colors
		resources["RailwayPrimary"] = Color.FromArgb("#1976D2");
		resources["RailwaySecondary"] = Color.FromArgb("#FF6F00");
		resources["RailwayAccent"] = Color.FromArgb("#00C853");
		resources["RailwayDanger"] = Color.FromArgb("#D32F2F");
		
		// Text colors
		resources["TextPrimary"] = Color.FromArgb("#FFFFFF");
		resources["TextSecondary"] = Color.FromArgb("#B0B0B0");
		resources["TextDisabled"] = Color.FromArgb("#707070");
		resources["TextOnPrimary"] = Color.FromArgb("#FFFFFF");
		resources["BorderColor"] = Color.FromArgb("#4D4D4D");
		
		// Legacy compatibility
		resources["PageBackgroundColor"] = Color.FromArgb("#121212");
		resources["FrameBackgroundColor"] = Color.FromArgb("#2D2D30");
		resources["Primary"] = Color.FromArgb("#1976D2");
		resources["White"] = Colors.White;
		resources["Gray200"] = Color.FromArgb("#C8C8C8");
		resources["Gray300"] = Color.FromArgb("#ACACAC");
		resources["Gray400"] = Color.FromArgb("#919191");
		resources["Gray600"] = Color.FromArgb("#404040");
		
		Resources = resources;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// ✅ Create MainPage AFTER App is initialized
		var mainPage = _services.GetRequiredService<MainPage>();
		return new Window(mainPage);
	}
}
