using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StreamMaster.Streams.Factories;

/// <summary>
/// Executes commands based on the provided profiles and manages process lifecycles.
/// </summary>
public class CommandExecutor(ILogger<CommandExecutor> logger) : ICommandExecutor, IDisposable
{
    private StreamWriter? errorWriter;
    private Process? _process;
    private bool _disposed;

    /// <inheritdoc/>
    public GetStreamResult ExecuteCommand(CommandProfileDto commandProfile, string streamUrl, string clientUserAgent, int? secondsIn, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            string? exec = FileUtil.GetExec(commandProfile.Command);
            if (exec == null)
            {
                logger.LogCritical("Command \"{command}\" not found", commandProfile.Command);
                return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.FileNotFound, Message = $"{commandProfile.Command} not found" });
            }

            string options = BuildCommand(commandProfile.Parameters, clientUserAgent, streamUrl, secondsIn);

            _process = new Process();
            ConfigureProcess(_process, exec, options);

            using var registration = cancellationToken.Register(() =>
            {
                logger.LogDebug("Cancellation requested for Stream process");
                GracefullyTerminateProcess();
            });

            cancellationToken.ThrowIfCancellationRequested();

            if (!_process.Start())
            {
                ProxyStreamError error = new() { ErrorCode = ProxyStreamErrorCode.ProcessStartFailed, Message = "Failed to start process" };
                logger.LogError("Error: {ErrorMessage}", error.Message);
                return new GetStreamResult(null, -1, error);
            }

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Process.Start("setpgrp", $"{_process.Id}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to set process group for {ProcessId}", _process.Id);
                }
            }

            string stderrFilePath = Path.Combine(BuildInfo.CommandErrorFolder, $"stderr_{_process.Id}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(stderrFilePath)!);
            errorWriter = new StreamWriter(stderrFilePath, append: true, Encoding.UTF8);

            // Clean up older logs to keep only the latest 10
            CleanupOldLogs(BuildInfo.CommandErrorFolder, 10);

            _process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (errorWriter) // Ensure thread-safe writes
                    {
                        errorWriter.WriteLine(e.Data);
                        errorWriter.Flush();
                    }
                }
            };
            _process.BeginErrorReadLine();
            _process.EnableRaisingEvents = true; // Ensure Exited event is raised
            _process.Exited += Process_Exited;

            stopwatch.Stop();
            logger.LogInformation("Opened command with args \"{options}\" in {ElapsedMilliseconds} ms", commandProfile.Command + ' ' + commandProfile.Parameters, stopwatch.ElapsedMilliseconds);

            return new GetStreamResult(_process.StandardOutput.BaseStream, _process.Id, null);
        }
        catch (OperationCanceledException ex)
        {
            ProxyStreamError error = new() { ErrorCode = ProxyStreamErrorCode.OperationCancelled, Message = "Operation was cancelled" };
            logger.LogError(ex, "Error: {ErrorMessage}", error.Message);
            return new GetStreamResult(null, -1, error);
        }
        catch (Exception ex)
        {
            ProxyStreamError error = new() { ErrorCode = ProxyStreamErrorCode.UnknownError, Message = ex.Message };
            logger.LogError(ex, "Error: {ErrorMessage}", error.Message);
            return new GetStreamResult(null, -1, error);
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Gracefully terminates the process using appropriate signals
    /// </summary>
    private void GracefullyTerminateProcess()
    {
        if (_process == null || _process.HasExited)
            return;

        try
        {
            logger.LogDebug("Attempting to gracefully terminate process {ProcessId}", _process.Id);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!_process.WaitForExit(3000))
                {
                    logger.LogWarning("Process {ProcessId} did not terminate gracefully, forcing kill", _process.Id);
                    _process.Kill(true);
                }
            }
            else
            {
                if (!_process.HasExited)
                {
                    Process.Start("kill", $"-TERM {_process.Id}");

                    if (!_process.WaitForExit(3000))
                    {
                        logger.LogWarning("Process {ProcessId} did not terminate after SIGTERM, sending SIGKILL", _process.Id);
                        Process.Start("kill", $"-KILL {_process.Id}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error gracefully terminating process {ProcessId}", _process.Id);

            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(true);
                }
            }
            catch (Exception killEx)
            {
                logger.LogError(killEx, "Failed to force kill process {ProcessId}", _process.Id);
            }
        }
    }

    private void CleanupOldLogs(string directoryPath, int maxLogsToKeep)
    {
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                return;
            }

            List<FileInfo> logFiles = [.. new DirectoryInfo(directoryPath)
                .GetFiles("stderr_*.log")
                .OrderByDescending(f => f.CreationTime)];

            if (logFiles.Count <= maxLogsToKeep)
            {
                return; // Nothing to clean up
            }

            foreach (FileInfo? file in logFiles.Skip(maxLogsToKeep))
            {
                try
                {
                    file.Delete();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to delete old log file: {FileName}", file.FullName);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up old logs in directory: {Directory}", directoryPath);
        }
    }

    private void Process_Exited(object? sender, EventArgs e)
    {
        if (_process != null)
        {
            try
            {
                _process.WaitForExit(); // Ensure process completes before disposing resources
                _process.CancelErrorRead();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error waiting for process to exit.");
            }
        }

        if (errorWriter != null)
        {
            try
            {
                errorWriter.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error disposing error writer.");
            }
        }

        try
        {
            _process?.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing process.");
        }
    }

    private static string BuildCommand(string command, string clientUserAgent, string streamUrl, int? secondsIn)
    {
        string s = secondsIn.HasValue ? $"-ss {secondsIn} " : "";

        command = command.Replace("{clientUserAgent}", '"' + clientUserAgent + '"')
                         .Replace("{streamUrl}", '"' + streamUrl + '"');

        if (secondsIn.HasValue)
        {
            int index = command.IndexOf("-i ");
            if (index >= 0)
            {
                command = command.Insert(index, s);
            }
        }

        return command;
    }

    private static void ConfigureProcess(Process process, string commandExec, string formattedArgs)
    {
        process.StartInfo.FileName = commandExec;
        process.StartInfo.Arguments = formattedArgs;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
        process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
        process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.Environment["SM_PROCESS_ID"] = process.Id.ToString();
            process.StartInfo.Environment["SM_PROCESS_TYPE"] = "STREAM";
        }

        process.EnableRaisingEvents = true;
    }

    /// <summary>
    /// Disposes the process and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        if (_process != null)
        {
            try
            {
                if (!_process.HasExited)
                {
                    GracefullyTerminateProcess();
                }
                _process.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error disposing process.");
            }
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}