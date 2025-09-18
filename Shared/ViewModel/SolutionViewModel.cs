namespace Moba.Shared.ViewModel;

using Moba.Backend.Model;

using System.Collections.ObjectModel;

public class SolutionViewModel
{
    public SolutionViewModel(Solution model)
    {
        Model = model;
        Projects = new ObservableCollection<Project>(model.Projects);
    }

    public Solution Model { get; }
    public ObservableCollection<Project> Projects { get; }
}