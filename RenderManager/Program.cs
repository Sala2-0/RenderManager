namespace RenderManager;
using Config;
using Utility;
using System.Text.Json;

internal class Program
{
    static async Task Main()
    {
        Configuration config = JsonSerializer.Deserialize<Configuration>(await File.ReadAllTextAsync(Configuration.GetConfigPath()))!;

        Console.WriteLine(
            "Render Manager - World of Warships\n" +
            "-help for information"
        );

        while (true)
        {
            if (!Directory.Exists(config.WorldOfWarships))
                Write.WriteRed("World of Warships root directory not configured! -config to configure");

            Console.Write("> ");
            var command = Console.ReadLine()?.Trim() ?? string.Empty;
            Console.WriteLine();

            if (command == "-config") Commands.Execute(ref config);
            else await Commands.ExecuteAsync(command, config);
        }
    }
}
