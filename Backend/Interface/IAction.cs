namespace Moba.Backend.Interface;

public interface IAction
{
    Guid Id { get; set; }
    string Name { get; set; }
    int Number { get; set; }
    IList<IAction> Actions { get; set; }
}