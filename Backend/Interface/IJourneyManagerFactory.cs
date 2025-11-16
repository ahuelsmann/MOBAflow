namespace Moba.Backend.Interface;

using Moba.Backend.Manager;
using Moba.Backend.Model;

public interface IJourneyManagerFactory
{
    JourneyManager Create(IZ21 z21, List<Journey> journeys, Model.Action.ActionExecutionContext? context = null);
}
