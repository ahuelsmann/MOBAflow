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
                Converters = { new ActionConverter() }
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
                    Converters = { new ActionConverter() }
                };

                var temp = JsonConvert.DeserializeObject<Solution>(json, settings);

                if (temp != null)
                {
                    temp.Name = path;
                    return temp;
                }
            }
        }
        return null;
    }
}