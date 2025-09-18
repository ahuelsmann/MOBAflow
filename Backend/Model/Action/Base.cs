namespace Moba.Backend.Model.Action;

using Moba.Backend.Enum;
using Moba.Backend.Interface;

public class Base 
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public virtual ActionType Type { get; set; }
    public IList<IAction> Actions { get; set; } = [];
}