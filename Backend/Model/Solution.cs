namespace Moba.Backend.Model;

using Newtonsoft.Json;

public class Solution
{
    public Solution()
    {
        Projects = [];
    }

    public List<Project> Projects { get; set; }

    public static void Save(string path, Solution? solution)
    {
        if (!string.IsNullOrEmpty(path) && solution != null)
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
                TypeNameHandling = TypeNameHandling.All
            };

            string json = JsonConvert.SerializeObject(solution, Formatting.Indented, settings);

            File.WriteAllText(path, json);
        }
    }

    public static Solution? Load(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = File.ReadAllText(path);
            if (!string.IsNullOrEmpty(json))
            {
                JsonSerializerSettings settings = new()
                {
                    TypeNameHandling = TypeNameHandling.All
                };

                var temp = JsonConvert.DeserializeObject<Solution>(json, settings);

                if (temp != null)
                {
                    return temp;
                }
            }
        }
        return null;
    }
}