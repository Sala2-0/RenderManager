namespace RenderManager.Config;
using Utility;
using System.Text.Json.Serialization;

public class Configuration
{
    [JsonPropertyName("world_of_warships")]
    public string? WorldOfWarships { get; set; }

    [JsonPropertyName("replays")]
    public string? Replays { get; set; }

    [JsonPropertyName("renders")]
    public string? Renders { get; set; }

    public static string GetConfigPath() => Path.Combine(Directory.GetCurrentDirectory(), "Config", "config.json");

    public static bool DirExists(string path)
    {
        if (!string.IsNullOrEmpty(path) || Directory.Exists(path)) return true;

        Write.WriteRed("Folder does not exist");
        return false;
    }

    public bool Configured()
    {
        if (!string.IsNullOrEmpty(WorldOfWarships) || !string.IsNullOrEmpty(Replays) || !string.IsNullOrEmpty(Renders))
            return true;

        Write.WriteRed("Paths not configured, run -config to configure.");
        return false;
    }
}
