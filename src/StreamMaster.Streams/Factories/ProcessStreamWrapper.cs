using System.Diagnostics;

namespace StreamMaster.Streams.Factories;

public class ProcessStreamWrapper : Stream
{
    private readonly Stream _baseStream;
    private readonly Process _process;
    private readonly Action<Process> _terminateAction;
    private readonly ILogger _logger;
    private bool _disposed = false;

    public ProcessStreamWrapper(Stream baseStream, Process process, Action<Process> terminateAction, ILogger logger)
    {
        _baseStream = baseStream ?? throw new ArgumentNullException(nameof(baseStream));
        _process = process ?? throw new ArgumentNullException(nameof(process));
        _terminateAction = terminateAction ?? throw new ArgumentNullException(nameof(terminateAction));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override bool CanRead => _baseStream.CanRead;
    public override bool CanSeek => _baseStream.CanSeek;
    public override bool CanWrite => _baseStream.CanWrite;
    public override long Length => _baseStream.Length;

    public override long Position
    {
        get => _baseStream.Position;
        set => _baseStream.Position = value;
    }

    public override void Flush() => _baseStream.Flush();

    public override int Read(byte[] buffer, int offset, int count) => _baseStream.Read(buffer, offset, count);

    public override long Seek(long offset, SeekOrigin origin) => _baseStream.Seek(offset, origin);

    public override void SetLength(long value) => _baseStream.SetLength(value);

    public override void Write(byte[] buffer, int offset, int count) => _baseStream.Write(buffer, offset, count);

    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _baseStream.ReadAsync(buffer, offset, count, cancellationToken);

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
       await _baseStream.ReadAsync(buffer, cancellationToken);

    public override async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger.LogDebug("DisposeAsync called for ProcessStreamWrapper for process {ProcessId}", _process.Id);

        try
        {
            if (!_process.HasExited)
            {
                _logger.LogDebug("Process {ProcessId} has not exited, initiating termination.", _process.Id);
                _terminateAction(_process);
                await _process.WaitForExitAsync();
            }
            else
            {
                _logger.LogDebug("Process {ProcessId} already exited.", _process.Id);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during process termination in ProcessStreamWrapper for process {ProcessId}", _process.Id);
        }

        await _baseStream.DisposeAsync();

        try
        {
            _process.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error disposing process object {ProcessId} in wrapper.", _process.Id);
        }

        _disposed = true;
        GC.SuppressFinalize(this);

        await base.DisposeAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _logger.LogDebug("Dispose(true) called for ProcessStreamWrapper for process {ProcessId}", _process?.Id ?? -1);
            try
            {
                if (_process != null && !_process.HasExited)
                {
                    _logger.LogDebug("Process {ProcessId} has not exited, initiating termination (sync).", _process.Id);
                    _terminateAction(_process);
                }
                else if (_process != null)
                {
                    _logger.LogDebug("Process {ProcessId} already exited (sync).", _process.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during process termination in ProcessStreamWrapper.Dispose for process {ProcessId}", _process?.Id ?? -1);
            }

            _baseStream?.Dispose();
            try
            {
                _process?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing process object {ProcessId} in wrapper (sync).", _process?.Id ?? -1);
            }
        }

        _disposed = true;
        base.Dispose(disposing);
    }
}