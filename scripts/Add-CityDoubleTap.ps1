$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

$handler = @'

    private void CityListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        // Add selected city as station to current journey on double-click
        if (ViewModel.SelectedCity != null && ViewModel.SelectedJourney != null)
        {
            var station = new Domain.Station { Name = ViewModel.SelectedCity.Name };
            var stationViewModel = new SharedUI.ViewModel.StationViewModel(station);
            ViewModel.SelectedJourney.Stations.Add(stationViewModel);
        }
    }
'@

$content = Get-Content "WinUI\View\EditorPage.xaml.cs" -Raw

if ($content -notmatch 'CityListView_DoubleTapped') {
    # Add before the closing #endregion and }
    $content = $content -replace '(\s*#endregion\s*}\s*)$', "$handler`r`n`$1"
    Set-Content "WinUI\View\EditorPage.xaml.cs" -Value $content -Encoding UTF8 -NoNewline
    Write-Host "Added DoubleTapped handler to EditorPage.xaml.cs" -ForegroundColor Green
} else {
    Write-Host "DoubleTapped handler already exists" -ForegroundColor Yellow
}
