namespace Moba.Backend.Manager;

using Moba.Backend.Interface;
using Moba.Backend.Model;

public class JourneyManagerFactory : IJourneyManagerFactory
{
    public JourneyManager Create(IZ21 z21, List<Journey> journeys, Model.Action.ActionExecutionContext? context = null)
        => new JourneyManager(z21, journeys, context);
}
