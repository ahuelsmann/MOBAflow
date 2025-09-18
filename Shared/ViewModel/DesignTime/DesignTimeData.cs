namespace Moba.Shared.ViewModel.DesignTime;

using Moba.Backend.Model;

public static class DesignTimeData
{
    public static Solution SampleSolution()
    {
        var sol = new Solution();
        var proj = new Project { Setting = new Setting { Name = "Default" } };
        proj.Voices.Add(new Voice { Name = "ElkeNeural", ProsodyRate = 1.0m });
        proj.Locomotives.Add(new Locomotive { Name = "BR101" });
        proj.Trains.Add(new Train { Name = "RE6" });
        sol.Projects.Add(proj);
        return sol;
    }
}
