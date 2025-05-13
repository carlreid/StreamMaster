using System.Diagnostics;
using System.IO.Pipelines;
using System.Text;
using CliWrap;
using CliWrap.Exceptions;

namespace StreamMaster.Streams.Factories
{
    /// <summary>
    /// Executes commands based on the provided profiles and manages process lifecycles using CliWrap.
    /// Streams stdout and logs stderr.
    /// </summary>
    public class CommandExecutor(ILogger<CommandExecutor> logger) : ICommandExecutor
    {
        private readonly ILogger<CommandExecutor> _logger = logger;
        private CancellationTokenSource? _processExitCts;
        private CommandTask<CommandResult>? _commandTask;
        private int _processId = -1;
        private string? _stderrFilePath;
        private bool _disposed;

        /// <inheritdoc/>
        public GetStreamResult ExecuteCommand(CommandProfileDto commandProfile, string streamUrl, string clientUserAgent, int? secondsIn, CancellationToken cancellationToken = default)
        {
            Directory.CreateDirectory(BuildInfo.CommandErrorFolder);

            Stopwatch stopwatch = Stopwatch.StartNew();
            Pipe pipe = new();

            _processExitCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var internalToken = _processExitCts.Token;

            try
            {
                string? execPath = FileUtil.GetExec(commandProfile.Command);
                if (string.IsNullOrEmpty(execPath))
                {
                    _logger.LogCritical("Command executable \"{Command}\" not found in PATH or specified location.", commandProfile.Command);
                    return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.FileNotFound, Message = $"{commandProfile.Command} not found" });
                }

                string arguments = BuildCommand(commandProfile.Parameters, clientUserAgent, streamUrl, secondsIn);

                _stderrFilePath = Path.Combine(BuildInfo.CommandErrorFolder, $"stderr_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}.log");

                _logger.LogInformation("Starting command: \"{Executable}\" with arguments: \"{Arguments}\". Logging stderr to: \"{StderrLogPath}\"",
                    execPath, arguments, _stderrFilePath);

                CleanupOldLogs(BuildInfo.CommandErrorFolder, 10);

                Command command = Cli.Wrap(execPath)
                    .WithArguments(arguments)
                    .WithStandardOutputPipe(PipeTarget.ToStream(pipe.Writer.AsStream(), autoFlush: true))
                    .WithStandardErrorPipe(PipeTarget.ToFile(_stderrFilePath))
                    .WithValidation(CommandResultValidation.None);

                _commandTask = command.ExecuteAsync(internalToken);

                _processId = _commandTask.ProcessId;
                _logger.LogInformation("Command (PID: {ProcessId}) started successfully in {ElapsedMilliseconds} ms. Arguments: {Arguments}",
                    _processId, stopwatch.ElapsedMilliseconds, arguments);

                _ = HandleCommandCompletionAsync(_commandTask, pipe.Writer, stopwatch, commandProfile, _processId, _stderrFilePath, internalToken);

                return new GetStreamResult(pipe.Reader.AsStream(), _processId, null);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogWarning("Command start explicitly cancelled before process execution.");
                pipe.Writer.Complete(new OperationCanceledException("Command start cancelled."));
                return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.OperationCancelled, Message = "Operation was cancelled before start" });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Error starting command \"{Command}\": {ErrorMessage}", commandProfile.Command, ex.Message);
                pipe.Writer.Complete(ex);
                TryDeleteFile(_stderrFilePath);
                return new GetStreamResult(null, -1, new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.UnknownError, Message = $"Failed to start command: {ex.Message}" });
            }
        }

        private async Task HandleCommandCompletionAsync(
            CommandTask<CommandResult> commandTask,
            PipeWriter pipeWriter,
            Stopwatch stopwatch,
            CommandProfileDto commandProfile,
            int processId,
            string? stderrFilePath,
            CancellationToken cancellationToken)
        {
            ProxyStreamError? error = null;
            Exception? completionException = null;
            try
            {
                CommandResult result = await commandTask;
                stopwatch.Stop();

                if (result.ExitCode == 0)
                {
                    _logger.LogInformation(
                        "Command {Command} (PID: {ProcessId}) completed successfully after {Duration}. Exit code: {ExitCode}",
                        commandProfile.Command, processId, stopwatch.Elapsed, result.ExitCode);
                }
                else
                {
                    _logger.LogWarning(
                       "Command {Command} (PID: {ProcessId}) exited after {Duration} with non-zero exit code: {ExitCode}. Check stderr log: {StderrLogPath}",
                       commandProfile.Command, processId, stopwatch.Elapsed, result.ExitCode, stderrFilePath);
                    error = new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.IoError, Message = $"Process exited with code {result.ExitCode}" };
                    completionException = new InvalidOperationException($"Process exited with code {result.ExitCode}. See log '{stderrFilePath}' for details.");
                }
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                stopwatch.Stop();
                _logger.LogInformation(
                    "Command {Command} (PID: {ProcessId}) was cancelled after {Duration}.",
                    commandProfile.Command, processId, stopwatch.Elapsed);
                error = new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.OperationCancelled, Message = "Command execution was cancelled." };
                completionException = ex;
            }
            catch (CommandExecutionException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Command {Command} (PID: {ProcessId}) failed execution after {Duration}. Exit code: {ExitCode}. Check stderr log: {StderrLogPath}. Error: {ErrorMessage}",
                    commandProfile.Command, processId, stopwatch.Elapsed, ex.ExitCode, stderrFilePath, ex.Message);
                error = new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.IoError, Message = $"Command execution failed: {ex.Message}" };
                completionException = ex;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "Command {Command} (PID: {ProcessId}) encountered an unexpected error after {Duration}. Check stderr log: {StderrLogPath}",
                    commandProfile.Command, processId, stopwatch.Elapsed, stderrFilePath);
                error = new ProxyStreamError { ErrorCode = ProxyStreamErrorCode.UnknownError, Message = $"Unexpected error: {ex.Message}" };
                completionException = ex;
            }
            finally
            {
                await pipeWriter.CompleteAsync(completionException);
                _logger.LogDebug("PipeWriter completed for PID {ProcessId}.", processId);
            }
        }

        private void CleanupOldLogs(string directoryPath, int maxLogsToKeep)
        {
            try
            {
                if (!Directory.Exists(directoryPath) || maxLogsToKeep <= 0)
                {
                    return;
                }

                var logFiles = new DirectoryInfo(directoryPath)
                    .GetFiles("stderr_*.log")
                    .OrderByDescending(f => f.LastWriteTime)
                    .Skip(maxLogsToKeep)
                    .ToList();

                if (!logFiles.Any())
                {
                    return;
                }

                _logger.LogDebug("Cleaning up {Count} old log files from {Directory}...", logFiles.Count, directoryPath);
                foreach (FileInfo file in logFiles)
                {
                    TryDeleteFile(file.FullName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during old log cleanup in directory: {Directory}", directoryPath);
            }
        }

        private void TryDeleteFile(string? filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;
            try
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogTrace("Deleted file: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete file: {FilePath}", filePath);
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

        /// <summary>
        /// Disposes the command executor, attempts to cancel the running process, and cleans up resources.
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
                _logger.LogDebug("Disposing CommandExecutor for PID {ProcessId}.", _processId);

                if (_processExitCts != null && !_processExitCts.IsCancellationRequested)
                {
                    _logger.LogInformation("Requesting cancellation for command (PID: {ProcessId}) via Dispose.", _processId);
                    try
                    {
                        _processExitCts.Cancel();
                    }
                    catch (ObjectDisposedException) { }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error cancelling CancellationTokenSource during Dispose for PID {ProcessId}.", _processId);
                    }
                }

                _processExitCts?.Dispose();
                _processExitCts = null;

                if (_processId > 0)
                {
                    try
                    {
                        Process? process = Process.GetProcessById(_processId);
                        if (!process.HasExited)
                        {
                            _logger.LogWarning("Process (PID: {ProcessId}) still running after cancellation signal. Forcing kill.", _processId);
                            process.Kill(entireProcessTree: true);
                            _logger.LogInformation("Force killed process (PID: {ProcessId}).", _processId);
                        }
                        process.Dispose();
                    }
                    catch (ArgumentException)
                    {
                        _logger.LogDebug("Process (PID: {ProcessId}) not found or already exited during Dispose.", _processId);
                    }
                    catch (InvalidOperationException)
                    {
                        _logger.LogDebug("Process (PID: {ProcessId}) exited before force kill attempt.", _processId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error during final process cleanup for PID {ProcessId} in Dispose.", _processId);
                    }
                }

                _logger.LogDebug("Finished disposing CommandExecutor for PID {ProcessId}.", _processId);
            }

            _disposed = true;
        }

        ~CommandExecutor()
        {
            Dispose(false);
        }
    }
}