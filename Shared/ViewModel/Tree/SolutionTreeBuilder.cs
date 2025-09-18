namespace Moba.Shared.ViewModel.Tree;

using Moba.Backend.Model;

using System.Collections.Generic;

public static class SolutionTreeBuilder
{
    public static TreeNodeViewModel Build(Solution solution)
    {
        var root = new TreeNodeViewModel("Solution", solution);
        foreach (var project in solution.Projects)
        {
            var projNode = new TreeNodeViewModel(project.Setting?.Name ?? "Project", project);
            root.Children.Add(projNode);

            projNode.Children.Add(CreateCollectionNode("SpeakerEngines", project.SpeakerEngines));
            projNode.Children.Add(CreateCollectionNode("Voices", project.Voices));
            projNode.Children.Add(CreateCollectionNode("Locomotives", project.Locomotives));
            projNode.Children.Add(CreateCollectionNode("PassengerWagons", project.PassengerWagons));
            projNode.Children.Add(CreateCollectionNode("Trains", project.Trains));
            projNode.Children.Add(CreateCollectionNode("Workflows", project.Workflows));
            projNode.Children.Add(CreateCollectionNode("Journeys", project.Journeys));
            projNode.Children.Add(CreateCollectionNode("Ips", project.Ips));
            projNode.Children.Add(new TreeNodeViewModel("Setting", project.Setting));
        }
        return root;
    }

    private static TreeNodeViewModel CreateCollectionNode<T>(string name, IList<T> list)
    {
        var node = new TreeNodeViewModel(name, list);
        foreach (var item in list)
        {
            node.Children.Add(new TreeNodeViewModel(item?.ToString() ?? typeof(T).Name, item!));
        }
        return node;
    }
}
