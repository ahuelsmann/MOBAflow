namespace Moba.Backend.Data;

using Newtonsoft.Json;

public class DataManager
{
    public DataManager()
    {
        Cities = [];
    }

    public List<City> Cities { get; set; }

    /// <summary>
    /// Save the data.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <param name="solution">Instance of DataManager to be saved.</param>
    public static async Task SaveAsync(string path, DataManager? solution)
    {
        if (!string.IsNullOrEmpty(path) && solution != null)
        {
            JsonSerializerSettings settings = new()
            {
                Formatting = Formatting.Indented,
            };
            string json = JsonConvert.SerializeObject(solution, settings);
            await File.WriteAllTextAsync(path, json).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Load the data.
    /// </summary>
    /// <param name="path">Expects the full path including file name.</param>
    /// <returns>Returns an instance of the loaded data as DataManager.</returns>
    public static async Task<DataManager?> LoadAsync(string path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            string json = await File.ReadAllTextAsync(path).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(json))
            {
                var temp = JsonConvert.DeserializeObject<DataManager>(json);
                return temp;
            }
        }
        return null;
    }
}