import re

with open('MOBAflow/View/JourneysPage.xaml', 'r', encoding='utf-8') as f:
    content = f.read()

# Replace the ColumnDefinitions
new_col_defs = """        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="250" MinWidth="150" />
            <ColumnDefinition Width="Auto" />
            <ColumnDefinition Width="*" />
        </Grid.ColumnDefinitions>"""
content = re.sub(r'<Grid\.ColumnDefinitions>.*?</Grid\.ColumnDefinitions>', new_col_defs, content, count=1, flags=re.DOTALL)

# Insert the first nested Grid before Stations
content = content.replace('        <!--  Stations (of selected Journey)  -->\n        <Grid Grid.Column="2" Padding="8">',
'''        <Grid Grid.Column="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="250" MinWidth="150" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <!--  Stations (of selected Journey)  -->
            <Grid Grid.Column="0" Padding="8">''')

# Insert the second nested Grid before City Library
content = content.replace('        <!--  City Library (CollapsibleColumn)  -->\n        <controls:CollapsibleColumnProperties\n            Grid.Column="4"',
'''        <Grid Grid.Column="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition x:Name="ColCityLib" Width="250" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" />
            </Grid.ColumnDefinitions>
            <!--  City Library (CollapsibleColumn)  -->
            <controls:CollapsibleColumnProperties
                Grid.Column="0"''')

# Insert the third nested Grid before Workflow Library
content = content.replace('        <!--  Workflow Library (CollapsibleColumn)  -->\n        <controls:CollapsibleColumnProperties\n            Grid.Column="6"',
'''        <Grid Grid.Column="2">
            <Grid.ColumnDefinitions>
                <ColumnDefinition x:Name="ColWorkflowLib" Width="250" />
                <ColumnDefinition Width="Auto" />
                <ColumnDefinition Width="*" MinWidth="200" />
            </Grid.ColumnDefinitions>
            <!--  Workflow Library (CollapsibleColumn)  -->
            <controls:CollapsibleColumnProperties
                Grid.Column="0"''')

# Fix Properties Grid Column
content = content.replace('        <!--  Properties Panel  -->\n        <Grid Grid.Column="8" Padding="8">',
'''        <!--  Properties Panel  -->
        <Grid Grid.Column="2" Padding="8">''')

# Replace the GridSplitters
splitters = """        <controls1:GridSplitter
            Grid.Column="1"
            Width="12"
            HorizontalAlignment="Center"
            ResizeBehavior="PreviousAndNext" />
        <controls1:GridSplitter
            Grid.Column="3"
            Width="12"
            HorizontalAlignment="Center"
            ResizeBehavior="PreviousAndNext" />
        <controls1:GridSplitter
            Grid.Column="5"
            Width="12"
            HorizontalAlignment="Center"
            ResizeBehavior="PreviousAndNext"
            Visibility="{x:Bind ViewModel.IsCityLibraryVisible, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
        <controls1:GridSplitter
            Grid.Column="7"
            Width="12"
            HorizontalAlignment="Center"
            ResizeBehavior="PreviousAndNext"
            Visibility="{x:Bind ViewModel.IsWorkflowLibraryVisible, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />"""

new_splitters = """        <!-- Splitters are distributed -->"""

content = content.replace(splitters, new_splitters)

content = content.replace("""        <!-- Splitters are distributed -->
    </Grid>
</Page>""",
"""                    <controls1:GridSplitter
                        Grid.Column="1"
                        Width="12"
                        HorizontalAlignment="Center"
                        ResizeBehavior="PreviousAndNext"
                        Visibility="{x:Bind ViewModel.IsWorkflowLibraryVisible, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
                </Grid>
                
                <controls1:GridSplitter
                    Grid.Column="1"
                    Width="12"
                    HorizontalAlignment="Center"
                    ResizeBehavior="PreviousAndNext"
                    Visibility="{x:Bind ViewModel.IsCityLibraryVisible, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
            </Grid>
            
            <controls1:GridSplitter
                Grid.Column="1"
                Width="12"
                HorizontalAlignment="Center"
                ResizeBehavior="PreviousAndNext" />
        </Grid>
        
        <controls1:GridSplitter
            Grid.Column="1"
            Width="12"
            HorizontalAlignment="Center"
            ResizeBehavior="PreviousAndNext" />
    </Grid>
</Page>""")

with open('MOBAflow/View/JourneysPage.xaml', 'w', encoding='utf-8') as f:
    f.write(content)
print("Done")
