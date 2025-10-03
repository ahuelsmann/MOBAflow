namespace Moba.Backend.Model;

using Newtonsoft.Json;

public class Solution
{
    public Solution()
    {
        Projects = [];
    }

    public List<Project> Projects { get; set; }

    public static async Task SaveAsync(string path, Solution? solution)
    {
        if (!string.IsNullOrEmpty(path) && solution != null)
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented
            };

            string json = JsonConvert.SerializeObject(solution, settings);

            await File.WriteAllTextAsync(path, json);
        }
    }

    public static async Task<Solution?> LoadAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path);
            if (!string.IsNullOrEmpty(json))
            {
                var temp = JsonConvert.DeserializeObject<Solution>(json);

                if (temp != null)
                {
                    return temp;
                }
            }
        }
        return null;
    }
}