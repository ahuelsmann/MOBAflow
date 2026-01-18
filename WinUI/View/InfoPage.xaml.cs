namespace Moba.WinUI.View;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;

using System.Collections.Generic;

public sealed partial class InfoPage : Page
{
    private Dictionary<TreeViewNode, string> _sections = [];

    public InfoPage()
    {
        this.InitializeComponent();
        Loaded += (s, e) => InitializePage();
    }

    private void InitializePage()
    {
        var root = new TreeViewNode { Content = "MOBAflow", IsExpanded = true };

        var topics = new[] { "About", "License", "Contact" };
        foreach (var topic in topics)
        {
            var node = new TreeViewNode { Content = topic };
            _sections[node] = topic;
            root.Children.Add(node);
        }

        NavigationTreeView.RootNodes.Add(root);
        ShowContent("About");
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
            "About" => "MOBAflow - Model Railway Control & Automation\nA comprehensive suite for Roco/Fleischmann Z21 command stations.\n\nFeatures:\n• Train Control\n• Signal Management\n• Workflow Automation\n• Multi-Platform Support",
            "License" => "MOBAflow is licensed under the MIT License.\n\nYou are free to:\n• Use commercially\n• Modify\n• Distribute\n\nWith the condition of:\n• License and copyright notice",
            "Contact" => "GitHub: github.com/ahuelsmann/MOBAflow\nIssues: github.com/ahuelsmann/MOBAflow/issues",
            _ => "Information"
        };

        var para = new Paragraph { Margin = new Thickness(0) };
        foreach (var line in content.Split('\n'))
        {
            para.Inlines.Add(new Run { Text = line });
            para.Inlines.Add(new LineBreak());
        }

        ContentRichTextBlock.Blocks.Add(para);
    }
}
