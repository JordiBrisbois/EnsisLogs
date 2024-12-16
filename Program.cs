using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            // Get the current directory
            string currentDirectory = Directory.GetCurrentDirectory();

            // Combine the current directory with the relative paths
            string executablePath = Path.Combine(currentDirectory, "GW2EI", "GuildWars2EliteInsights.exe");
            string configPath = Path.Combine(currentDirectory, "config", "EIP.conf");
            string logsPath = Path.Combine(currentDirectory, "logs");
            string pythonScriptPath = Path.Combine(currentDirectory, "parser", "TW5_parse_top_stats_detailed.py");
            string htmlOutputPath = Path.Combine(currentDirectory, "parser", "example_output", "TW5_Top_Stat_Parse.html");

            // Get the current date in MMddyy format
            string dateString = DateTime.Now.ToString("MMddyy");
            // Generate a unique six-digit number
            string uniqueNumber = new Random().Next(100000, 999999).ToString();
            // Combine date and unique number
            string dateFolderName = $"{dateString}_{uniqueNumber}";

            // Combine the current directory with the date-named paths
            string logsDonePath = Path.Combine(currentDirectory, "logsdone", dateFolderName);
            string uselessPath = Path.Combine(logsDonePath, "useless");

            // Ensure directories exist
            EnsureDirectoryExists(logsDonePath);
            EnsureDirectoryExists(uselessPath);

            // Validate necessary files and directories
            ValidatePaths(executablePath, configPath, logsPath, pythonScriptPath);

            // Get all .zevtc files from the logs directory
            string[] logFiles = Directory.GetFiles(logsPath, "*.zevtc");

            if (logFiles.Length == 0)
            {
                Console.WriteLine("No .zevtc files found in the logs directory.");
                return;
            }

            // Construct the arguments string
            string arguments = $"-c {configPath} " + string.Join(" ", logFiles.Select(f => $"\"{f}\""));

            // Run the external executable and wait for it to finish
            await RunProcessAsync(executablePath, arguments);
            Console.WriteLine("External process has finished execution.");

            // Run the Python script and wait for it to finish
            await RunProcessAsync("python", $"{pythonScriptPath} {logsPath}");
            Console.WriteLine("Python script has finished execution.");

            // Move files and clean up
            MoveFiles(logsPath, logsDonePath, uselessPath);
            OpenFileExplorerWithSelectedFiles(logsDonePath, "*.tid");

            // Open the HTML output in the default web browser
            OpenHtmlFile(htmlOutputPath);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }

    static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }

    static void ValidatePaths(params string[] paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
                continue;
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"File not found: {path}");
            }
        }
    }

    static async Task RunProcessAsync(string fileName, string arguments)
    {
        var processStartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using (var process = new Process { StartInfo = processStartInfo })
        {
            process.Start();
            await process.WaitForExitAsync();
        }
    }

    static void MoveFiles(string sourcePath, string logsDonePath, string uselessPath)
    {
        foreach (string file in Directory.GetFiles(sourcePath))
        {
            string fileName = Path.GetFileName(file);
            string destinationPath = file.EndsWith(".tid") ?
                Path.Combine(logsDonePath, fileName) :
                Path.Combine(uselessPath, fileName);

            File.Move(file, destinationPath);
        }

        // Clear the logs directory
        foreach (string file in Directory.GetFiles(sourcePath))
        {
            File.Delete(file);
        }
    }

    static void OpenFileExplorerWithSelectedFiles(string directoryPath, string searchPattern)
    {
        string[] files = Directory.GetFiles(directoryPath, searchPattern);

        if (files.Length > 0)
        {
            string explorerArgs = $"/select,{string.Join(",", files.Select(f => $"\"{f}\""))}";
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = explorerArgs,
                UseShellExecute = true
            });

            Console.WriteLine("File Explorer has been opened with .tid files selected.");
        }
    }

    static void OpenHtmlFile(string htmlOutputPath)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = htmlOutputPath,
            UseShellExecute = true
        });

        Console.WriteLine("HTML output has been opened in the default web browser.");
    }
}
