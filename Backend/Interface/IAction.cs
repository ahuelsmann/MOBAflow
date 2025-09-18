namespace Moba.Backend.Interface;

using Moba.Backend.Model;

public interface IAction
{
    Guid Id { get; set; }
    string Name { get; set; }
    int Number { get; set; }
    IList<IAction> Actions { get; set; }
    Task ExecuteAsync(Journey journey, Station stop);
}