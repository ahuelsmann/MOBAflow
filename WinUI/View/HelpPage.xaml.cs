namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

using System.Collections.Generic;

public sealed partial class HelpPage : Page
{
    private Dictionary<TreeViewNode, string> _sections = [];

    public HelpPage()
    {
        this.InitializeComponent();
        Loaded += (s, e) => InitializePage();
    }

    private void InitializePage()
    {
        var root = new TreeViewNode { Content = "Getting Started", IsExpanded = true };

        var topics = new[] { "Installation", "First Steps", "Configuration" };
        foreach (var topic in topics)
        {
            var node = new TreeViewNode { Content = topic };
            _sections[node] = topic;
            root.Children.Add(node);
        }

        NavigationTreeView.RootNodes.Add(root);
        ShowContent("Getting Started");
    }

    private void OnNavigationItemInvoked(TreeView sender, TreeViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is TreeViewNode node && _sections.TryGetValue(node, out var section))
        {
            ShowContent(section);
        }
    }

    private void ShowContent(string section)
    {
        HeaderTextBlock.Text = section;
        ContentRichTextBlock.Blocks.Clear();

        var content = section switch
        {
            "Getting Started" => "Welcome to MOBAflow. Start by connecting to your Z21 command station.",
            "Installation" => "Download and install MOBAflow from GitHub. Requires Windows 10/11 and .NET 10.",
            "First Steps" => "1. Connect to Z21\n2. Create a Solution\n3. Add Locomotives\n4. Configure Layout",
            "Configuration" => "Configure your layout, feedback points, and automation workflows.",
            _ => "Select a topic from the menu."
        };

        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var line in content.Split('\n'))
        {
            para.Inlines.Add(new Run { Text = line });
            if (!line.EndsWith("\n"))
                para.Inlines.Add(new LineBreak());
        }

        ContentRichTextBlock.Blocks.Add(para);
    }
}
