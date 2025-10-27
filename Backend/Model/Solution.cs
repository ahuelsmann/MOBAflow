namespace Moba.Backend.Model;

using Converter;

using Newtonsoft.Json;

public class Solution
{
    public Solution()
    {
        Projects = [];
    }

    public string Name { get; set; } = string.Empty;

    public List<Project> Projects { get; set; }

    public static async Task SaveAsync(string path, Solution? solution)
    {
        if (!string.IsNullOrEmpty(path) && solution != null)
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                Converters = { new ActionConverter() },
                // Ignore reference loops (e.g., circular references)
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // Do NOT ignore null values - important for workflow references!
                // If Flow = null, this must be saved
                NullValueHandling = NullValueHandling.Include
            };

            string json = JsonConvert.SerializeObject(solution, settings);

            await File.WriteAllTextAsync(path, json);
        }
    }

    public async Task<Solution?> LoadAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path);
            if (!string.IsNullOrEmpty(json))
            {
                JsonSerializerSettings settings = new()
                {
                    Converters = { new ActionConverter() },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                };

                var temp = JsonConvert.DeserializeObject<Solution>(json, settings);

                if (temp != null)
                {
                    temp.Name = path;

                    // Restore workflow references in stations
                    RestoreWorkflowReferences(temp);

                    return temp;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Restores workflow references in stations after loading
    /// </summary>
    private static void RestoreWorkflowReferences(Solution solution)
    {
        System.Diagnostics.Debug.WriteLine("🔄 RestoreWorkflowReferences START");

        foreach (var project in solution.Projects)
        {
            System.Diagnostics.Debug.WriteLine($"  Project: {project.Name}");
            System.Diagnostics.Debug.WriteLine($"  Workflows: {project.Workflows.Count}");

            foreach (var wf in project.Workflows)
            {
                System.Diagnostics.Debug.WriteLine($"    - {wf.Name} (Id: {wf.Id})");
            }

            // Iterate through all journeys and stations
            foreach (var journey in project.Journeys)
            {
                System.Diagnostics.Debug.WriteLine($"  Journey: {journey.Name}");

                foreach (var station in journey.Stations)
                {
                    System.Diagnostics.Debug.WriteLine($"    Station: {station.Name}");
                    System.Diagnostics.Debug.WriteLine($"      Flow BEFORE: {station.Flow?.Name ?? "null"} (Id: {station.Flow?.Id})");

                    // Flow is now a temporary Workflow object with only the GUID
                    // Replace it with the real reference from project.Workflows
                    if (station.Flow != null)
                    {
                        Workflow? matchingWorkflow = null;

                        // Search by GUID
                        if (station.Flow.Id != Guid.Empty)
                        {
                            matchingWorkflow = project.Workflows
                                                .FirstOrDefault(w => w.Id == station.Flow.Id);
                            System.Diagnostics.Debug.WriteLine($"      Searching by GUID: {station.Flow.Id}");
                        }

                        // Fallback: Search by name (old files)
                        if (matchingWorkflow == null && !string.IsNullOrEmpty(station.Flow.Name))
                        {
                            matchingWorkflow = project.Workflows
                                .FirstOrDefault(w => w.Name == station.Flow.Name);
                            System.Diagnostics.Debug.WriteLine($"      Searching by Name: {station.Flow.Name}");
                        }

                        if (matchingWorkflow != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"      ✅ Found: {matchingWorkflow.Name} (Id: {matchingWorkflow.Id})");
                            station.Flow = matchingWorkflow;
                            System.Diagnostics.Debug.WriteLine($"      Flow AFTER: {station.Flow.Name} (Id: {station.Flow.Id})");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"    ❌ NO MATCHING WORKFLOW FOUND!");
                            station.Flow = null;
                        }
                    }
                }
            }
        }

        System.Diagnostics.Debug.WriteLine("🔄 RestoreWorkflowReferences END");
    }
}