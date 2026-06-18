// Copyright (c) 2026 Andreas Huelsmann. Licensed under MIT. See LICENSE and README.md for details.
namespace Moba.MAUI;

using View;

public partial class AppShell
{
    public AppShell(AppTabHostPage tabHostPage)
    {
        InitializeComponent();

        Items.Add(new ShellContent
        {
            Content = tabHostPage,
            Route = "Main"
        });
    }
}