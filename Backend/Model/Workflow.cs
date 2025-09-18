namespace Moba.Backend.Model;

public partial class Workflow
{
    public Workflow()
    {
        Name = "New Flow";
        Actions = [];
    }

    public string Name { get; set; }
    public List<Action.Base> Actions { get; set; }
}