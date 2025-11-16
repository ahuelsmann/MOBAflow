using Moba.SharedUI.ViewModel;

namespace Moba.Test.SharedUI;

public class TreeNodeViewModelTests
{
    [Test]
    public void RefreshDisplayName_UpdatesFromDataContext()
    {
        var node = new TreeNodeViewModel();
        var model = new { Name = "Old" };
        node.DataContext = model;
        node.DisplayName = "Old";

        // Change underlying model using reflection (anonymous type is immutable), so create a new object
        node.DataContext = new { Name = "NewName" };
        node.RefreshDisplayName();

        Assert.That(node.DisplayName, Is.EqualTo("NewName"));
    }

    [Test]
    public void ChildrenCollection_InitiallyEmpty()
    {
        var node = new TreeNodeViewModel();
        Assert.That(node.Children, Is.Not.Null);
        Assert.That(node.Children.Count, Is.EqualTo(0));
    }
}
