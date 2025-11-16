namespace Moba.SharedUI.Service;

using Moba.Backend.Model;
using Moba.SharedUI.ViewModel;

public interface IJourneyViewModelFactory
{
    JourneyViewModel Create(Journey model);
}
