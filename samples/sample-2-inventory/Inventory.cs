using System.Text.Json;

// Simple class to implement inventory storage
// It loads data from json file and saves data on dispose

public class Inventory : Dictionary<string, int>, IDisposable
{
    private readonly string _filepath;

    public Inventory(string filepath) : base(StringComparer.OrdinalIgnoreCase) 
    {
        _filepath = filepath;
        Load(_filepath);
    }

    public void Dispose()
    {
        Save(_filepath);
    }

    internal string ToJson()
    {
        return JsonSerializer.Serialize<Dictionary<string, int>>(this, new JsonSerializerOptions { WriteIndented = true });
    }

    public void Load(string filepath)
    {
        if (File.Exists(filepath))
        {
            var json = File.ReadAllText(filepath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new();
            foreach (var item in dict)
                Add(item.Key, item.Value);
        }
    }

    public void Save(string filepath)
    {
        var json = ToJson();
        File.WriteAllText(filepath, json);
    }
}
