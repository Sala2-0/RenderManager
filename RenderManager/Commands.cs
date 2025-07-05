using System.Diagnostics;
using RenderManager.Utility;

namespace RenderManager;
using Config;
using System.Text.Json;
class Commands
{
    private static readonly string[] BannedChars = ["\\", "/", ":", "*", "?", "\"", "<", ">", "|"];

    public static void Execute(ref Configuration config)
    {
        Console.Write(
            "Copy and paste World of Warships root folder\n" +
            "If you have the game via WGC, should be something like: C:\\Games\\World_of_Warships\n" +
            "If you have the game via Steam, should be something like: C:\\Program (86)\\Steam\\steamapps\\common\\World_of_Warships" +
            "\n> "
        );
        string wowsRootPath = Console.ReadLine()?.Trim('\"') ?? string.Empty;

        if (!CheckNull(wowsRootPath)) return;

        Configuration.DirExists(wowsRootPath);
        config.WorldOfWarships = wowsRootPath;

        Console.Write(
            "\nCopy and paste World of Warships replays directory\n" +
            "Most likely you're gonna have different folders with versions as names, if so copy the latest version instead of replays root directory" +
            "\n> "
        );
        string replaysPath = Console.ReadLine()?.Trim('\"') ?? string.Empty;

        if (!CheckNull(replaysPath)) return;

        Configuration.DirExists(replaysPath);
        config.Replays = replaysPath;

        if (!Directory.Exists($"{config.WorldOfWarships}\\Renders"))
            Directory.CreateDirectory($"{config.WorldOfWarships}\\Renders");

        config.Renders = $"{config.WorldOfWarships}\\Renders";

        string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Configuration.GetConfigPath(), json);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("\nFinished configuration");
        Console.ResetColor();
        Console.WriteLine($"Renders will be stored at \"{config.Renders}\"");
    }

    public static async Task ExecuteAsync(string command, Configuration config)
    {
        if (string.IsNullOrEmpty(command)) return;

        if (command == "-help")
        {
            Console.WriteLine(
                "-mkdir: Create a new folder\n" +
                "-rmdir: Remove a folder\n" +
                "-config: Configure the application settings\n" +
                "-render: Render a single replay\n" +
                "-liverender: Render games as you play\n" +
                "-list: List of current existing folders\n" +
                "-q: Quit\n"
            );
        }

        else if (command == "-mkdir")
        {
            Console.Write("Name:\n> ");
            string folder = Console.ReadLine()?.Trim() ?? string.Empty;

            if (!config.Configured()) return;

            if (string.IsNullOrEmpty(folder) || !CheckDirName(folder))
            {
                Write.WriteRed(
                    "Invalid name. Folder name cannot be empty or contain " +
                    "the following characters: " + string.Join(", ", BannedChars)
                );
                return;
            }

            var path = Path.Combine(config.Renders!, folder);

            if (Directory.Exists(path))
            {
                Write.WriteRed($"Folder \"{folder}\" already exists.");
                return;
            }

            try
            {
                Directory.CreateDirectory(path);
                Write.WriteGreen($"Folder \"{folder}\" has been created successfully");
            }
            catch (Exception ex)
            {
                Write.WriteRed($"Error creating folder: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
            }
        }

        else if (command == "-rmdir")
        {
            if (!config.Configured()) return;

            Console.Write("Folder:\n> ");
            string folderName = Console.ReadLine()?.Trim('\"') ?? string.Empty;

            if (!CheckNull(folderName)) return;

            var folder = Path.Combine(config.Renders!, folderName);

            if (!config.Configured()) return;

            Configuration.DirExists(folder);

            try
            {
                Directory.Delete(folder, true);
                Write.WriteGreen($"Folder \"{folder}\" has been removed successfully.");
            }
            catch (Exception ex)
            {
                Write.WriteRed($"Error removing folder: {ex.Message}");
            }
        }

        else if (command == "-render")
        {
            if (!config.Configured()) return;

            Console.Write("Copy and paste file path.\n> ");

            StartExplorer(!string.IsNullOrEmpty(config.Replays) ? config.Replays : "C:\\");

            string filePath = Console.ReadLine()?.Trim('\"') ?? string.Empty;

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                Write.WriteRed("Invalid file path");
                return;
            }

            if (Path.GetExtension(filePath) != ".wowsreplay")
            {
                Write.WriteRed("File is not a WoWs replay");
                return;
            }

            Console.Write("\nDestination folder\n> ");
            string outputFolder = Console.ReadLine()?.Trim('\"') ?? string.Empty;

            Console.Write("\nOutput name (blank for default name)\n> ");
            string outputName = Console.ReadLine()?.Trim('\"') ?? string.Empty;

            if (!CheckDirName(outputName))
            {
                Write.WriteRed("Name cannot contain the following characters: " + string.Join(", ", BannedChars));
                return;
            }

            // Förvald namn är filnamnet utan extension
            string fileName = !string.IsNullOrEmpty(outputName)
                ? outputName
                : Path.GetFileNameWithoutExtension(filePath);

            string outputPath = Path.Combine(config.Renders!, outputFolder);

            if (string.IsNullOrEmpty(outputPath) || !Directory.Exists(outputPath))
            {
                Write.WriteRed("Invalid output path.");
                return;
            }


            if (File.Exists($"{Path.Combine(outputPath, fileName)}.mp4"))
            {
                Write.WriteRed("Output name already exists");
                return;
            }

            await Renderer.Render(filePath, Path.Combine(outputPath, fileName));
        }

        else if (command == "-liverender")
        {
            if (!config.Configured()) return;

            Console.Write("Destination folder\n> ");
            string outputFolder = Console.ReadLine()?.Trim('\"') ?? string.Empty;

            if (!CheckNull(outputFolder)) return;

            var path = Path.Combine(config.Renders!, outputFolder);

            if (!Directory.Exists(path))
            {
                Write.WriteRed($"Folder \"{outputFolder}\" does not exist.");
                return;
            }

            await LiveRenderAsync(path, config);
        }

        else if (command == "-list")
        {
            if (!config.Configured()) return;
            Console.WriteLine("Current folders:");

            var directories = Directory.GetDirectories(config.Renders!);
            if (directories.Length == 0)
            {
                Write.WriteRed("No folders found.");
                return;
            }

            foreach (var dir in directories)
                Console.WriteLine($"- {Path.GetFileName(dir)}");

            Console.WriteLine();
        }

        else if (command == "-q")
            Environment.Exit(0);

        else
            Write.WriteRed("Unknown command");
    }

    private static bool CheckDirName(string name)
    {
        foreach (string ch in BannedChars)
            if (name.Contains(ch)) return false;

        return true;
    }

    private static async Task LiveRenderAsync(string destPath, Configuration config)
    {
        Console.WriteLine($"Renders will be stored at {destPath}");
        Console.WriteLine("Press ESC to stop live render");

        bool active = true;
        string lastReplayBeforeStart = "";

        foreach (var file in Directory.GetFiles(config.Replays!, "*.wowsreplay"))
        {
            FileInfo fileInfo = new FileInfo(file);
            if (string.IsNullOrEmpty(lastReplayBeforeStart) || fileInfo.CreationTime > new FileInfo(lastReplayBeforeStart).CreationTime)
                lastReplayBeforeStart = file;
        }

        var inputTask = Task.Run(() =>
        {
            while (active)
                if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape)
                    active = false;
        });

        DateTime lastReplayTime = new FileInfo(lastReplayBeforeStart).CreationTime;

        do
        {
            string? nextReplay = null;
            DateTime latestTime = lastReplayTime;

            foreach (string file in Directory.GetFiles(config.Replays!, "*.wowsreplay"))
            {
                if (file == "temp.wowsreplay") continue;

                FileInfo fileInfo = new FileInfo(file);

                if (fileInfo.CreationTime <= lastReplayTime) continue;

                if (fileInfo.CreationTime > latestTime)
                {
                    latestTime = fileInfo.CreationTime;
                    nextReplay = file;
                }
            }

            if (nextReplay != null)
            {
                Write.WriteYellow("New game detected");
                lastReplayTime = latestTime;

                await Task.Delay(3000);

                string outputName = Path.GetFileNameWithoutExtension(nextReplay);
                string outputPath = Path.Combine(destPath, outputName);
                await Renderer.Render(nextReplay, outputPath);
            }

            await Task.Delay(1000);
        } while (active);

        await inputTask;
    }

    public static void StartExplorer(string path) => Process.Start(new ProcessStartInfo
    {
        FileName = "explorer.exe",
        Arguments = path,
        UseShellExecute = true
    });

    public static bool CheckNull(string? input)
    {
        if (!string.IsNullOrEmpty(input)) return true;

        Write.WriteRed("Input cannot be empty");
        return false;
    }
}
