// Copyright (c) 2025-2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.Smart;

public partial class App : Application
{
	private readonly MainPage _mainPage;

	public App(MainPage mainPage)
	{
		InitializeComponent();
		_mainPage = mainPage;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_mainPage);
	}
}
