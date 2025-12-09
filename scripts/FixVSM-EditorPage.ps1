$ErrorActionPreference='Stop'
[Console]::OutputEncoding=[Text.Encoding]::UTF8

$file = "C:\Repos\ahuelsmann\MOBAflow\WinUI\View\EditorPage.xaml"
$content = Get-Content $file -Raw

# Remove ALL existing VisualStateManager blocks
$content = $content -replace '(?s)\s*<VisualStateManager\.VisualStateGroups>.*?</VisualStateManager\.VisualStateGroups>\s*', ''

# Define the VSM block to insert (properly indented for Page level)
$vsmBlock = @'

    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="PropertiesStates">
            <VisualState x:Name="Expanded">
                <VisualState.Setters>
                    <Setter Target="PropertiesContent.Visibility" Value="Visible" />
                </VisualState.Setters>
                <VisualState.Storyboard>
                    <Storyboard>
                        <DoubleAnimation
                            Storyboard.TargetName="PropertiesContent"
                            Storyboard.TargetProperty="Opacity"
                            From="0.0"
                            To="1.0"
                            Duration="0:0:0.25">
                            <DoubleAnimation.EasingFunction>
                                <CubicEase EasingMode="EaseOut" />
                            </DoubleAnimation.EasingFunction>
                        </DoubleAnimation>
                    </Storyboard>
                </VisualState.Storyboard>
            </VisualState>

            <VisualState x:Name="Collapsed">
                <VisualState.Setters>
                    <Setter Target="PropertiesContent.Visibility" Value="Collapsed" />
                </VisualState.Setters>
                <VisualState.Storyboard>
                    <Storyboard>
                        <DoubleAnimation
                            Storyboard.TargetName="PropertiesContent"
                            Storyboard.TargetProperty="Opacity"
                            From="1.0"
                            To="0.0"
                            Duration="0:0:0.20">
                            <DoubleAnimation.EasingFunction>
                                <CubicEase EasingMode="EaseIn" />
                            </DoubleAnimation.EasingFunction>
                        </DoubleAnimation>
                    </Storyboard>
                </VisualState.Storyboard>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
'@

# Insert VSM block after </Page.Resources>
$content = $content -replace '(    </Page\.Resources>)', "`$1`r`n$vsmBlock"

# Save
Set-Content $file -Value $content -Encoding UTF8 -NoNewline

Write-Host "✅ VisualStateManager fixed in EditorPage.xaml (Page level)"
