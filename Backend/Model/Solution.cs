namespace Moba.Backend.Model;

using Converter;

using Newtonsoft.Json;

/// <summary>
/// A solution folder that can contain multiple MOBA projects.
/// The solution represents the root element. It is the first element within an object tree.
/// </summary>
public class Solution
{
    public Solution()
    {
        Projects = [];
    }

    public string Name { get; set; } = string.Empty;

    public List<Project> Projects { get; set; }

    /// <summary>
    /// Save the solution.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <param name="solution">Instance of the solution to be saved.</param>
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

    /// <summary>
    /// Load the solution.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <returns>Returns an instance of the loaded solution.</returns>
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

            // Restore workflow references in stations and platforms
           RestoreWorkflowReferences(temp);

       return temp;
         }
    }
        }
        return null;
    }

/// <summary>
    /// Restores workflow references in stations and platforms after loading
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

   // Restore station workflow reference
         var restoredStationFlow = RestoreWorkflowReference(station.Flow, project.Workflows, "Station", station.Name);
    station.Flow = restoredStationFlow;

        // Restore platform workflow references
       if (station.Platforms.Count > 0)
            {
        System.Diagnostics.Debug.WriteLine($"      Platforms: {station.Platforms.Count}");
   foreach (var platform in station.Platforms)
    {
       var restoredPlatformFlow = RestoreWorkflowReference(platform.Flow, project.Workflows, "Platform", platform.Name);
     platform.Flow = restoredPlatformFlow;
   }
      }
         }
            }
 }

        System.Diagnostics.Debug.WriteLine("🔄 RestoreWorkflowReferences END");
    }

    /// <summary>
    /// Helper method to restore a single workflow reference
    /// </summary>
    /// <returns>The restored workflow reference or null if not found</returns>
    private static Workflow? RestoreWorkflowReference(
        Workflow? currentFlow,
        List<Workflow> projectWorkflows,
   string entityType,
        string entityName)
    {
      System.Diagnostics.Debug.WriteLine($"      Flow BEFORE: {currentFlow?.Name ?? "null"} (Id: {currentFlow?.Id})");

        // Flow is now a temporary Workflow object with only the GUID
   // Replace it with the real reference from project.Workflows
        if (currentFlow != null)
        {
      Workflow? matchingWorkflow = null;

     // Search by GUID
          if (currentFlow.Id != Guid.Empty)
            {
         matchingWorkflow = projectWorkflows.FirstOrDefault(w => w.Id == currentFlow.Id);
    System.Diagnostics.Debug.WriteLine($"        Searching by GUID: {currentFlow.Id}");
            }

            // Fallback: Search by name (old files)
   if (matchingWorkflow == null && !string.IsNullOrEmpty(currentFlow.Name))
 {
           matchingWorkflow = projectWorkflows.FirstOrDefault(w => w.Name == currentFlow.Name);
       System.Diagnostics.Debug.WriteLine($"        Searching by Name: {currentFlow.Name}");
            }

   if (matchingWorkflow != null)
  {
    System.Diagnostics.Debug.WriteLine($"        ✅ Found for {entityType} '{entityName}': {matchingWorkflow.Name} (Id: {matchingWorkflow.Id})");
                return matchingWorkflow;
            }
       else
     {
     System.Diagnostics.Debug.WriteLine($" ❌ NO MATCHING WORKFLOW FOUND for {entityType} '{entityName}'!");
      return null;
 }
        }

  return null;
    }
}