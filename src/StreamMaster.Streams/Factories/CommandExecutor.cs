using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StreamMaster.Streams.Factories;

/// <summary>
/// Executes commands based on the provided profiles and manages process lifecycles.
/// </summary>
public class CommandExecutor(ILogger<CommandExecutor> logger) : ICommandExecutor, IDisposable
{
    private bool _disposed;

    /// <inheritdoc/>
    public GetStreamResult ExecuteCommand(CommandProfileDto commandProfile, string streamUrl, string clientUserAgent, int? secondsIn, CancellationToken cancellationToken = default)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        Process? currentProcess = null;
        StreamWriter? currentErrorWriter = null;
        Stream? wrappedStream = null;

        try
        {
            string? exec = FileUtil.GetExec(commandProfile.Command);
            if (exec == null)
            {
                logger.LogCritical("Command \"{command}\" not found", commandProfile.Command);
                return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.FileNotFound, Message = $"{commandProfile.Command} not found" });
            }
            string options = BuildCommand(commandProfile.Parameters, clientUserAgent, streamUrl, secondsIn);

            currentProcess = new Process();
            ConfigureProcess(currentProcess, exec, options);

            using var registration = cancellationToken.Register(() =>
            {
                Process? processToCancel = currentProcess;
                logger.LogDebug("Cancellation requested for Stream process {ProcessId}", processToCancel?.Id ?? -1);
                if (processToCancel != null)
                {
                    GracefullyTerminateProcessInternal(processToCancel, logger);
                }
            });

            cancellationToken.ThrowIfCancellationRequested(); // Check cancellation after registration

            if (!currentProcess.Start())
            {
                currentProcess.Dispose();
                currentProcess = null;
                ProxyStreamError error = new() { ErrorCode = ProxyStreamErrorCode.ProcessStartFailed, Message = "Failed to start process" };
                logger.LogError("Error: {ErrorMessage}", error.Message);
                return new GetStreamResult(null, -1, error);
            }

            logger.LogInformation("Process {ProcessId} started successfully.", currentProcess.Id);

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                try
                {
                    Process.Start("setpgrp", $"{currentProcess.Id}");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to set process group for {ProcessId}", currentProcess.Id);
                }
            }

            string stderrFilePath = Path.Combine(BuildInfo.CommandErrorFolder, $"stderr_{currentProcess.Id}.log");
            Directory.CreateDirectory(Path.GetDirectoryName(stderrFilePath)!);
            currentErrorWriter = new StreamWriter(stderrFilePath, append: true, Encoding.UTF8);
            CleanupOldLogs(BuildInfo.CommandErrorFolder, 10);

            currentProcess.ErrorDataReceived += (sender, e) =>
            {
                StreamWriter? writer = currentErrorWriter;
                if (writer != null && !string.IsNullOrWhiteSpace(e.Data))
                {
                    lock (writer)
                    {
                        try
                        {
                            writer.WriteLine(e.Data);
                            writer.Flush();
                        }
                        catch (ObjectDisposedException) { /* Ignore if writer was disposed concurrently */ }
                        catch (Exception ex) { logger.LogError(ex, "Error writing stderr for Process {ProcessId}", (sender as Process)?.Id ?? -1); }
                    }
                }
            };
            currentProcess.BeginErrorReadLine();
            currentProcess.EnableRaisingEvents = true;

            currentProcess.Exited += (sender, e) => Process_Exited(sender as Process, currentErrorWriter);

            stopwatch.Stop();
            logger.LogInformation("Opened command with args \"{options}\" for ProcessId {ProcessId} in {ElapsedMilliseconds} ms", commandProfile.Command + ' ' + commandProfile.Parameters, currentProcess.Id, stopwatch.ElapsedMilliseconds);

            Action<Process> terminateDelegate = (processToTerminate) =>
            {
                GracefullyTerminateProcessInternal(processToTerminate, logger);
            };

            wrappedStream = new ProcessStreamWrapper(currentProcess.StandardOutput.BaseStream, currentProcess, terminateDelegate, logger);

            var processId = currentProcess.Id;
            var streamToReturn = wrappedStream;
            currentProcess = null; // Ownership transferred to wrapper
            currentErrorWriter = null; // Ownership (disposal) transferred to Exited event/wrapper

            return new GetStreamResult(streamToReturn, processId, null);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("ExecuteCommand cancelled for streamUrl: {StreamUrl}", streamUrl);
            CleanupFailedExecution(currentProcess, currentErrorWriter);
            return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.OperationCancelled, Message = "Operation was cancelled" });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error executing command for streamUrl {StreamUrl}: {ErrorMessage}", streamUrl, ex.Message);
            CleanupFailedExecution(currentProcess, currentErrorWriter);
            return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.UnknownError, Message = ex.Message });
        }
    }

    private void CleanupFailedExecution(Process? process, StreamWriter? writer)
    {
        if (process != null)
        {
            logger.LogWarning("Cleaning up process {ProcessId} due to execution failure.", process.Id);
            // Attempt termination before disposing
            GracefullyTerminateProcessInternal(process, logger);
            try { process.Dispose(); }
            catch (Exception ex) { logger.LogError(ex, "Error disposing failed process {ProcessId}.", process.Id); }
        }
        if (writer != null)
        {
            try { writer.Dispose(); }
            catch (Exception ex) { logger.LogError(ex, "Error disposing failed error writer."); }
        }
    }

    private static void GracefullyTerminateProcessInternal(Process? processToTerminate, ILogger log)
    {
        if (processToTerminate == null)
        {
            log.LogDebug("GracefullyTerminateProcessInternal called with null process.");
            return;
        }

        try
        {
            bool alreadyExited = false;
            try
            {
                alreadyExited = processToTerminate.HasExited;
            }
            catch (InvalidOperationException)
            {
                log.LogWarning("Error checking HasExited for process {ProcessId} (may already be disposed or inaccessible). Assuming exited.", processToTerminate.Id);
                alreadyExited = true;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                log.LogWarning(ex, "Error checking HasExited for process {ProcessId}. Assuming exited.", processToTerminate.Id);
                alreadyExited = true;
            }

            if (alreadyExited)
            {
                log.LogDebug("GracefullyTerminateProcessInternal: Process {ProcessId} already exited.", processToTerminate.Id);
                return;
            }

            log.LogDebug("Attempting to gracefully terminate process {ProcessId}", processToTerminate.Id);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                log.LogDebug("Waiting for process {ProcessId} to exit (Windows)...", processToTerminate.Id);
                if (!processToTerminate.WaitForExit(3000))
                {
                    log.LogWarning("Process {ProcessId} did not terminate gracefully after wait, forcing kill", processToTerminate.Id);
                    bool exitedBeforeKill = false;
                    try { exitedBeforeKill = processToTerminate.HasExited; } catch { }
                    if (!exitedBeforeKill)
                    {
                        processToTerminate.Kill(true);
                    }
                }
                else
                {
                    log.LogDebug("Process {ProcessId} exited gracefully after wait (Windows).", processToTerminate.Id);
                }
            }
            else
            {
                log.LogDebug("Sending SIGTERM to process {ProcessId}...", processToTerminate.Id);
                Process.Start("kill", $"-TERM {processToTerminate.Id}");
                if (!processToTerminate.WaitForExit(3000))
                {
                    log.LogWarning("Process {ProcessId} did not terminate after SIGTERM, sending SIGKILL", processToTerminate.Id);
                    bool exitedBeforeKill = false;
                    try { exitedBeforeKill = processToTerminate.HasExited; } catch { }
                    if (!exitedBeforeKill)
                    {
                        log.LogDebug("Sending SIGKILL to process {ProcessId}...", processToTerminate.Id);
                        Process.Start("kill", $"-KILL {processToTerminate.Id}");
                        processToTerminate.WaitForExit(500);
                    }
                }
                else
                {
                    log.LogDebug("Process {ProcessId} terminated after SIGTERM.", processToTerminate.Id);
                }
            }
        }
        catch (InvalidOperationException ex)
        {
            log.LogWarning(ex, "Error terminating process {ProcessId}. It might have already exited.", processToTerminate.Id);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            log.LogError(ex, "Win32Error during termination of process {ProcessId}", processToTerminate.Id);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error gracefully terminating process {ProcessId}", processToTerminate.Id);
            try
            {
                bool exitedBeforeKill = false;
                try { exitedBeforeKill = processToTerminate.HasExited; } catch { }
                if (!exitedBeforeKill)
                {
                    log.LogWarning("Forcing kill on process {ProcessId} due to prior termination errors.", processToTerminate.Id);
                    processToTerminate.Kill(true);
                }
            }
            catch (Exception killEx)
            {
                log.LogError(killEx, "Failed to force kill process {ProcessId}", processToTerminate.Id);
            }
        }
        finally
        {
            try
            {
                bool finalExitCheck = false;
                try { finalExitCheck = processToTerminate.HasExited; } catch { }
                if (!finalExitCheck)
                {
                    log.LogWarning("Process {ProcessId} termination logic completed, but HasExited is still false.", processToTerminate.Id);
                }
                else
                {
                    log.LogDebug("Process {ProcessId} confirmed exited after termination logic.", processToTerminate.Id);
                }
            }
            catch { /* Ignore final state check errors */ }
        }
    }

    private void Process_Exited(Process? process, StreamWriter? writer)
    {
        if (process == null)
        {
            logger.LogWarning("Process_Exited called with null process.");
            return;
        }

        logger.LogDebug("Process {ProcessId} Exited event received.", process.Id);
        try
        {
            if (!process.WaitForExit(1000))
            {
                logger.LogWarning("Process {ProcessId} Exited event received, but WaitForExit(1000) timed out.", process.Id);
            }

            try
            {
                if (process.StartInfo.RedirectStandardError)
                {
                    process.CancelErrorRead();
                }
            }
            catch (InvalidOperationException) { /* Ignore if already detached */ }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error waiting for process {ProcessId} to fully exit in Exited event.", process.Id);
        }

        // Dispose the specific writer associated with this process exit
        if (writer != null)
        {
            try
            {
                logger.LogDebug("Disposing error writer for Process {ProcessId}.", process.Id);
                writer.Dispose();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error disposing error writer for Process {ProcessId}.", process.Id);
            }
        }

        // Dispose the specific process handle that exited
        try
        {
            logger.LogDebug("Disposing process object {ProcessId}.", process.Id);
            process.Dispose();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error disposing process object {ProcessId} in Exited event.", process.Id);
        }
    }

    private static string BuildCommand(string command, string clientUserAgent, string streamUrl, int? secondsIn)
    {
        // (Implementation as before)
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
            process.StartInfo.Environment["SM_PROCESS_TYPE"] = "STREAM";
        }
        process.EnableRaisingEvents = true;
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
                return;
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

    /// <summary>
    /// Disposes managed resources. Primarily for fallback; lifetime should be tied to the wrapped stream.
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            logger.LogDebug("CommandExecutor Dispose({Disposing}) called. This is a fallback.", disposing);
        }

        _disposed = true;
    }
}