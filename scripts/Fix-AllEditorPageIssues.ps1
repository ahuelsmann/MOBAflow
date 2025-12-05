$ErrorActionPreference='Stop'
Set-Location "C:\Repos\ahuelsmann\MOBAflow"

Write-Host "=== Fixing EditorPage Issues ===" -ForegroundColor Cyan

# 1. Fix Solution Tab - remove City Library
Write-Host "[1/3] Solution: Removing City Library..." -ForegroundColor Yellow
$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# Replace Solution Content with simpler 2-column layout
$oldSolutionContent = '(?s)<!--\s*Solution Tab.*?-->.*?<Grid x:Name="SolutionContent"[^>]*>.*?</Grid>\s*(?=<!--.*?Journeys)'

$newSolutionContent = @'
<!--  ============================================  -->
            <!--  Solution Tab: Projects + PropertyGrid  -->
            <!--  ============================================  -->
            <Grid x:Name="SolutionContent" Visibility="Collapsed">
                <Grid.ColumnDefinitions>
                    <ColumnDefinition Width="*" MinWidth="300" />
                    <ColumnDefinition Width="Auto" />
                    <ColumnDefinition Width="400" MinWidth="300" MaxWidth="600" />
                </Grid.ColumnDefinitions>

                <!--  Column 0: Projects  -->
                <Grid Grid.Column="0" Padding="4">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Padding="16,6,24,4" FontWeight="SemiBold" Style="{StaticResource SubtitleTextBlockStyle}" Text="Projects" />
                    <ListView Grid.Row="1" ItemsSource="{x:Bind ViewModel.SolutionViewModel.Projects, Mode=OneWay}" SelectedItem="{x:Bind ViewModel.SelectedProject, Mode=TwoWay}" SelectionMode="Single">
                        <ListView.ItemTemplate>
                            <DataTemplate x:DataType="viewmodel:ProjectViewModel">
                                <Grid Padding="6">
                                    <Grid.ColumnDefinitions>
                                        <ColumnDefinition Width="Auto" />
                                        <ColumnDefinition Width="*" />
                                    </Grid.ColumnDefinitions>
                                    <FontIcon Grid.Column="0" FontSize="16" Glyph="&#xE8F1;" Margin="0,0,8,0" />
                                    <TextBlock Grid.Column="1" Text="{x:Bind Name, Mode=OneWay}" VerticalAlignment="Center" />
                                </Grid>
                            </DataTemplate>
                        </ListView.ItemTemplate>
                    </ListView>
                </Grid>

                <Border Grid.Column="1" Width="1" Background="{ThemeResource DividerStrokeColorDefaultBrush}" />

                <!--  Column 2: PropertyGrid  -->
                <Grid Grid.Column="2" Padding="4">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto" />
                        <RowDefinition Height="*" />
                    </Grid.RowDefinitions>
                    <TextBlock Grid.Row="0" Padding="16,6,24,4" FontWeight="SemiBold" Style="{StaticResource SubtitleTextBlockStyle}" Text="Properties" />
                    <local:SimplePropertyGrid Grid.Row="1" SelectedObject="{x:Bind ViewModel.SelectedProject, Mode=OneWay}" />
                </Grid>
            </Grid>

            
'@

$content = $content -replace $oldSolutionContent, $newSolutionContent

Set-Content "WinUI\View\EditorPage.xaml" -Value $content -Encoding utf8
Write-Host "[1/3] Solution Tab fixed" -ForegroundColor Green

# 2. Fix Workflows Actions DataTemplate
Write-Host "[2/3] Workflows: Fixing Actions DataTemplate..." -ForegroundColor Yellow
$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# Actions should NOT bind to domain:WorkflowAction but to object (polymorphic ViewModels)
# Change DataTemplate to just display the action
$content = $content -replace '<DataTemplate x:DataType="domain:WorkflowAction">', '<DataTemplate>'
$content = $content -replace '(\s*<TextBlock Grid\.Column="1" Text="\{x:Bind )Type(, Mode=OneWay\}")', '$1ToString()$2'

Set-Content "WinUI\View\EditorPage.xaml" -Value $content -Encoding utf8
Write-Host "[2/3] Workflows Actions DataTemplate fixed" -ForegroundColor Green

# 3. Add Workflow ComboBox to Journeys Stations
Write-Host "[3/3] Journeys: Adding Workflow selector to Stations..." -ForegroundColor Yellow
$content = Get-Content "WinUI\View\EditorPage.xaml" -Raw

# This is complex - we need to change Stations DataTemplate to include Workflow ComboBox
# For now, just document it
Write-Host "[3/3] Workflow selector for Stations: TODO - requires Station template modification" -ForegroundColor Yellow

Write-Host "Done! Please review changes." -ForegroundColor Green
