using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Serialization;

using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.XmltvXml;

namespace StreamMaster.Domain.Common;

public sealed class FileUtil
{
    /// <summary>
    /// Validates whether a file path is valid for the current operating system.
    /// </summary>
    /// <param name="filePath">The file path to validate.</param>
    /// <returns>True if the file path is valid; otherwise, false.</returns>
    public static bool IsValidFilePath(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false; // Null, empty, or whitespace paths are invalid
        }

        try
        {
            // Check for invalid characters
            char[] invalidChars = Path.GetInvalidPathChars();
            if (filePath.Any(ch => invalidChars.Contains(ch)))
            {
                return false;
            }

            // Check for additional platform-specific constraints
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows-specific rules
                if (filePath.Length > 260) // Max path length in many cases
                {
                    return false;
                }

                string directoryName = Path.GetDirectoryName(filePath) ?? string.Empty;
                string fileName = Path.GetFileName(filePath);

                if (directoryName.Any(ch => Path.GetInvalidPathChars().Contains(ch)) ||
                    fileName.Any(ch => Path.GetInvalidFileNameChars().Contains(ch)))
                {
                    return false;
                }
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // Unix-like systems allow almost any characters except '\0' and '/'
                if (filePath.Contains('\0') || filePath.Any(ch => ch == '/'))
                {
                    return false;
                }
            }

            // Ensure it's a valid absolute or relative path
            string fullPath = Path.GetFullPath(filePath);
        }
        catch
        {
            return false; // Any exception indicates an invalid path
        }

        return true;
    }

    /// <summary>
    /// Checks if the file path is valid and whether the file exists.
    /// </summary>
    /// <param name="filePath">The file path to validate and check for existence.</param>
    /// <returns>True if the path is valid and the file exists; otherwise, false.</returns>
    public static bool IsFilePathValidAndExists(string filePath)
    {
        return IsValidFilePath(filePath) && File.Exists(filePath);
    }

    /// <summary>
    /// Searches for the specified executable name in predefined directories.
    /// </summary>
    /// <param name="executableName">The name of the executable to locate.</param>
    /// <returns>The full path to the executable if found, otherwise null.</returns>
    public static string? GetExec(string executableName)
    {
        if (string.IsNullOrEmpty(executableName))
        {
            throw new ArgumentException("Executable name cannot be null or empty.", nameof(executableName));
        }

        // List of directories to search for the executable
        string[] directoriesToSearch =
        [
            string.Empty, // Current directory
            BuildInfo.AppDataFolder,
            "/usr/local/bin",
            "/usr/bin",
            "/bin"
        ];

        foreach (string? directory in directoriesToSearch)
        {
            string execPath = Path.Combine(directory, executableName);

            if (File.Exists(execPath))
            {
                return execPath;
            }

            if (File.Exists(execPath + ".exe"))
            {
                return execPath + ".exe";
            }
        }

        // If nothing found, attempt to use .NET Process to locate the executable.
        try
        {
            using Process process = new();

            // Set up process to just get the path without actually running the program
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                process.StartInfo.FileName = "where";
                process.StartInfo.Arguments = executableName;
            }
            else
            {
                process.StartInfo.FileName = "which";
                process.StartInfo.Arguments = executableName;
            }

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            if (process.Start())
            {
                string? output = process.StandardOutput.ReadLine();
                process.WaitForExit();

                if (!string.IsNullOrEmpty(output) && File.Exists(output))
                {
                    return output;
                }
            }
        }
        catch
        {
            // Silently fail if the process approach doesn't work
        }

        return null;
    }

    public static void WriteJSON(string fileName, string jsonText, string? directory = null)
    {
        string jsonPath = Path.Combine(directory ?? BuildInfo.AppDataFolder, fileName);
        File.WriteAllText(jsonPath, jsonText);
    }

    public static string EncodeToMD5(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            throw new ArgumentNullException(nameof(url), "URL cannot be null or empty.");
        }
        byte[] hashBytes = System.Security.Cryptography.MD5.HashData(Encoding.UTF8.GetBytes(url));
        return Convert.ToHexStringLower(hashBytes);
    }

    public static async Task<bool> WaitForFileAsync(string filePath, int timeoutSeconds, int checkIntervalMilliseconds, CancellationToken cancellationToken)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return true;
            }
        }
        catch (Exception)
        {
            return false;
        }

        bool didReport = false;
        using CancellationTokenSource timeoutTokenSource = new(TimeSpan.FromSeconds(timeoutSeconds));
        using CancellationTokenSource linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutTokenSource.Token);

        try
        {
            string? directoryPath = Path.GetDirectoryName(filePath);

            if (directoryPath != null && !Directory.Exists(directoryPath))
            {
                // Wait for the directory to exist
                while (!Directory.Exists(directoryPath))
                {
                    if (!didReport)
                    {
                        didReport = true;
                        Debug.WriteLine("Waited on {directoryPath}", directoryPath);
                    }
                    await Task.Delay(checkIntervalMilliseconds, linkedTokenSource.Token).ConfigureAwait(false);
                }
            }

            // Wait for the file to exist
            while (!File.Exists(filePath))
            {
                await Task.Delay(checkIntervalMilliseconds, linkedTokenSource.Token).ConfigureAwait(false);
            }

            return true;
        }
        catch (OperationCanceledException) when (timeoutTokenSource.Token.IsCancellationRequested)
        {
            // Timeout has occurred
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // External cancellation request
            throw;
        }
    }

    private static readonly char[] separator = [' '];

    public static string CleanUpFileName(string fullName)
    {
        // Remove double spaces, trim, and replace spaces with underscores
        fullName = string.Join("_", fullName.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));

        // Ensure the file name doesn't start or end with an underscore
        if (fullName.StartsWith('_'))
        {
            fullName = fullName.TrimStart('_');
        }

        if (fullName.EndsWith('_'))
        {
            fullName = fullName.TrimEnd('_');
        }
        return fullName;
    }

    public static string BytesToString(long bytes)
    {
        string[] unit = ["", "K", "M", "G", "T"];
        for (int i = 0; i < unit.Length; ++i)
        {
            double calc;
            if ((calc = bytes / Math.Pow(1024, i)) < 1024)
            {
                return $"{calc:N3} {unit[i]}B";
            }
        }
        return "0 bytes";
    }

    public static bool WriteXmlFile(XMLTV xmltv, string filepath)
    {
        try
        {
            XmlSerializer serializer = new(typeof(XMLTV));

            XmlSerializerNamespaces ns = new();
            ns.Add("", "");
            using StreamWriter writer = new(filepath, false, Encoding.UTF8);
            serializer.Serialize(writer, xmltv, ns);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to write file \"{filepath}\". Exception:{ReportExceptionMessages(ex)}");
        }
        return false;
    }

    public static string ReportExceptionMessages(Exception ex)
    {
        string ret = string.Empty;
        Exception? innerException = ex;
        do
        {
            ret += $" {innerException.Message} ";
            innerException = innerException.InnerException;
        } while (innerException != null);
        return ret;
    }

    public static async Task Backup(int? versionsToKeep = null)
    {
        Setting? setting = SettingsHelper.GetSetting<Setting>(BuildInfo.SettingsFile);
        if (setting?.BackupEnabled != true)
        {
            return;
        }

        try
        {
            versionsToKeep ??= SettingsHelper.GetSetting<Setting>(BuildInfo.SettingsFile)?.BackupVersionsToKeep ?? 5;
            using Process process = new();
            process.StartInfo.FileName = "/bin/bash";
            process.StartInfo.Arguments = $"/usr/local/bin/backup.sh {versionsToKeep}";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            _ = process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrEmpty(output))
            {
                Console.WriteLine($"Backup Output: {output}");
            }

            if (!string.IsNullOrEmpty(error))
            {
                Console.WriteLine($"Backup Error: {error}");
            }

            if (process.ExitCode == 0)
            {
                Console.WriteLine("Backup executed successfully.");
            }
            else
            {
                Console.WriteLine($"Backup execution failed with exit code: {process.ExitCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Backup Exception occurred: {ex.Message}");
        }
    }
}