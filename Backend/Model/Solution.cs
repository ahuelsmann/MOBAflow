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
                // ✅ Ignoriere Referenz-Loops (z.B. bei zirkulären Referenzen)
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                // ✅ Null-Werte NICHT ignorieren - wichtig für Workflow-Referenzen!
                // Wenn Flow = null ist, muss das auch gespeichert werden
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

                    // ✅ Workflow-Referenzen in Stations wiederherstellen
                    RestoreWorkflowReferences(temp);

                    return temp;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Stellt Workflow-Referenzen in Stations wieder her nach dem Laden
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

            // Iteriere durch alle Journeys und Stations
            foreach (var journey in project.Journeys)
            {
                System.Diagnostics.Debug.WriteLine($"  Journey: {journey.Name}");

                foreach (var station in journey.Stations)
                {
                    System.Diagnostics.Debug.WriteLine($"    Station: {station.Name}");
                    System.Diagnostics.Debug.WriteLine($"      Flow BEFORE: {station.Flow?.Name ?? "null"} (Id: {station.Flow?.Id})");

                    // ✅ Flow ist jetzt ein temporäres Workflow-Objekt mit nur der GUID
                    // Ersetze es durch die echte Referenz aus project.Workflows
                    if (station.Flow != null)
                    {
                        Workflow? matchingWorkflow = null;

                        // Suche nach GUID
                        if (station.Flow.Id != Guid.Empty)
                        {
                            matchingWorkflow = project.Workflows
                           .FirstOrDefault(w => w.Id == station.Flow.Id);
                            System.Diagnostics.Debug.WriteLine($"      Searching by GUID: {station.Flow.Id}");
                        }

                        // Fallback: Suche nach Namen (alte Dateien)
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