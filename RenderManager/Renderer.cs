namespace RenderManager;
using Utility;
using System.Diagnostics;
internal class Renderer
{
    public const string FileNotFound = "FileNotFoundError";
    public const string Completed = "Done";

    public static async Task Render(string filePath, string outputPath)
    {
        // Utmatning är i form av ErrorOutput
        var renderer = new ProcessStartInfo
        {
            FileName = Path.Combine(Directory.GetCurrentDirectory(), "Renderer", "Python", "python.exe"),
            Arguments = $"-m render --replay {filePath}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
        };

        using var process = Process.Start(renderer);

        if (process == null)
        {
            Write.WriteRed("Failed to start the renderer process.");
            return;
        }

        Task<string> outputTask = process.StandardError.ReadToEndAsync();
        int spinnerIndex = 0;

        while (!outputTask.IsCompleted)
        {
            Console.Write($"\rRendering {Spinner.Spin(spinnerIndex++)}    ");
            if (spinnerIndex == 4) spinnerIndex = 0;
            await Task.Delay(100);
        }

        string output = await outputTask;
        await process.WaitForExitAsync();

        if (output.Contains(FileNotFound))
            Write.WriteRed("\rError: File not found");

        else if (output.Contains(Completed))
        {
            string dir = Path.GetDirectoryName(filePath)!;
            string fileName = Path.GetFileNameWithoutExtension(filePath);

            if (File.Exists($"{outputPath}.mp4"))
                File.Delete($"{outputPath}.mp4");

            // Radera JSON filen som kommer med
            File.Delete($"{dir}\\{fileName}-builds.json");

            Directory.Move($"{dir}\\{fileName}.mp4", $"{outputPath}.mp4");

            Write.WriteGreen("\rSuccessfully rendered replay");
        }
    }
}
