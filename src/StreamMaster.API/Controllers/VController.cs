using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace StreamMaster.API.Controllers;

[V1ApiController("v")]
public class VsController(ILogger<VsController> logger, IProfileService profileService, ICommandExecutor commandExecutor, IVideoService videoService, IStreamGroupService streamGroupService, IChannelService channelService) : Controller
{
    [Authorize(Policy = "SGLinks")]
    [HttpGet]
    [HttpHead]
    [Route("{encodedIds}")]
    [Route("{encodedIds}.ts")]
    [Route("{streamGroupProfileId}/{smChannelId}")]
    [Route("{streamGroupProfileId}/{smChannelId}.ts")]
    public async Task<ActionResult> HandleStreamRequest(
    string? encodedIds = null,
    int? smChannelId = null,
    int? streamGroupProfileId = null,
    CancellationToken cancellationToken = default)
    {
        int? streamGroupId = null;

        try
        {
            if (!string.IsNullOrEmpty(encodedIds))
            {
                (streamGroupId, streamGroupProfileId, smChannelId) = await streamGroupService.DecodeProfileIdSMChannelIdFromEncodedAsync(encodedIds);
            }
            else if (smChannelId.HasValue && streamGroupProfileId.HasValue)
            {
                streamGroupId = await streamGroupService.GetStreamGroupIdFromSGProfileIdAsync(streamGroupProfileId).ConfigureAwait(false);
            }
            else
            {
                logger.LogWarning("Invalid request: Missing required parameters.");
                return NotFound();
            }

            // Prepare response headers
            HttpContext.Response.ContentType = "video/mp2t";
            HttpContext.Response.Headers.CacheControl = "no-cache";
            HttpContext.Response.Headers.Pragma = "no-cache";
            HttpContext.Response.Headers.Expires = "0";
            // Set the Content-Disposition header to specify the filename
            string fileName = $"{encodedIds ?? "stream"}.ts";
            HttpContext.Response.Headers.ContentDisposition = $"inline; filename=\"{fileName}\"";

            StreamResult streamResult = await videoService.AddClientToChannelAsync(HttpContext, streamGroupId, streamGroupProfileId, smChannelId, cancellationToken);

            if (!string.IsNullOrEmpty(streamResult.RedirectUrl))
            {
                logger.LogInformation("Channel with ChannelId {channelId} is redirecting to {redirectUrl}", smChannelId, streamResult.RedirectUrl);
                return Redirect(streamResult.RedirectUrl);
            }

            if (streamResult.ClientConfiguration == null)
            {
                logger.LogWarning("Channel with ChannelId {channelId} not found or failed. Name: {name}", smChannelId, streamResult.ClientConfiguration?.SMChannel.Name ?? "Unknown");
                return NotFound();
            }

            // Register client stopped event
            streamResult.ClientConfiguration.OnClientStopped += (sender, args) =>
            {
                //logger.LogInformation("Client {UniqueRequestId} stopped. Name: {name}", streamResult.ClientConfiguration.UniqueRequestId, streamResult.ClientConfiguration.SMChannel.Name);
                _ = channelService.UnRegisterClientAsync(streamResult.ClientConfiguration.UniqueRequestId);
            };

            // Register for dispose to ensure cleanup
            HttpContext.Response.RegisterForDispose(new UnregisterClientOnDispose(channelService, streamResult.ClientConfiguration, logger));
            await streamResult.ClientConfiguration.ClientCompletionSource.Task.ConfigureAwait(false);

            return new EmptyResult();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing stream request. Parameters: encodedIds={encodedIds}, smChannelId={smChannelId}, streamGroupProfileId={streamGroupProfileId}", encodedIds, smChannelId, streamGroupProfileId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request.");
        }
    }

    [Authorize(Policy = "SGLinks")]
    [HttpGet]
    [HttpHead]
    [Route("c/{encodedStreamLocation}")]
    [Route("c/{encodedStreamLocation}.ts")]
    public async Task<ActionResult> HandleSStreamRequest(
    string encodedStreamLocation,
    CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(encodedStreamLocation))
            {
                logger.LogWarning("Invalid request: Missing required parameters.");
                return BadRequest("Missing required parameters.");
            }

            string withoutExtension = Path.GetFileNameWithoutExtension(encodedStreamLocation);
            string fileName = withoutExtension?.FromUrlSafeBase64String();

            if (string.IsNullOrWhiteSpace(fileName) || !System.IO.File.Exists(fileName))
            {
                logger.LogWarning("File not found: {FileName}", fileName);
                return NotFound();
            }

            HttpContext.Response.ContentType = "video/mp2t";
            HttpContext.Response.Headers.CacheControl = "no-cache";
            HttpContext.Response.Headers.Pragma = "no-cache";
            HttpContext.Response.Headers.Expires = "0";
            HttpContext.Response.Headers.ContentDisposition = $"inline; filename=\"{Path.GetFileName(fileName)}\"";

            CommandProfileDto commandProfile = profileService.GetCommandProfile("SMFFMPEGLocal");
            commandProfile.Parameters = "-hide_banner -loglevel error -i {streamUrl} -map 0 -c copy -f mpegts pipe:1";

            using (var cancellationTokenSource = new CancellationTokenSource())
            {
                using (var linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken,
                    HttpContext.RequestAborted,
                    cancellationTokenSource.Token))
                {
                    linkedCTS.Token.Register(() => logger.LogWarning("Linked cancellation token triggered."));
                    cancellationToken.Register(() => logger.LogWarning("Request cancellation token triggered."));
                    HttpContext.RequestAborted.Register(() => logger.LogWarning("Request aborted by client."));

                    GetStreamResult res = commandExecutor.ExecuteCommand(commandProfile, fileName, "", null, linkedCTS.Token);

                    if (res == null || res.Stream == null || res.ProcessId == -1)
                    {
                        logger.LogWarning("Failed to execute command for file: {FileName}", fileName);
                        return NotFound();
                    }

                    try
                    {
                        byte[] buffer = new byte[16384];
                        while (true)
                        {
                            int bytesRead = await res.Stream.ReadAsync(buffer, linkedCTS.Token);
                            if (bytesRead > 0)
                            {
                                logger.LogDebug("Read {BytesRead} bytes from stream.", bytesRead);
                                await HttpContext.Response.Body.WriteAsync(
                                    buffer.AsMemory(0, bytesRead),
                                    linkedCTS.Token);
                            }
                            else break;
                        }
                        logger.LogInformation("Streaming completed successfully for file: {FileName}.");
                    }
                    catch (OperationCanceledException)
                    {
                        // Handle cancellation
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error occurred while streaming the file: {FileName}");
                        if (!HttpContext.Response.HasStarted)
                            HttpContext.Response.StatusCode = 500;
                    }
                    finally
                    {
                        logger.LogInformation("Cleaning up resources for file: {FileName}");
                        KillProcessById(res.ProcessId);
                        await HttpContext.Response.Body.FlushAsync(HttpContext.RequestAborted);
                        await res.Stream.DisposeAsync();
                    }
                    return new EmptyResult();
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing stream request. encodedStreamLocation={EncodedStreamLocation}", encodedStreamLocation);
            return !HttpContext.Response.HasStarted
                ? StatusCode(500, "An error occurred while processing the request.")
                : new EmptyResult();
        }
    }

    public static void KillProcessById(int processId)
    {
        try
        {
            Process processById = Process.GetProcessById(processId);
            processById.Kill();
            processById.WaitForExit();
        }
        catch (ArgumentException ex)
        {
            Console.WriteLine($"No process with ID {processId} is running.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("An error occurred while killing the process: " + ex.Message);
        }
    }

    private class UnregisterClientOnDispose(IChannelService channelService, IClientConfiguration config, ILogger logger) : IDisposable
    {
        private readonly IChannelService _channelService = channelService;
        private readonly IClientConfiguration _config = config;

        public void Dispose()
        {
            DisposeAsync().GetAwaiter().GetResult();
        }

        private async Task DisposeAsync()
        {
            try
            {
                logger.LogInformation("Unregistered Client {UniqueRequestId} {name} disposing", _config.UniqueRequestId, _config.SMChannel.Name);

                // Complete the HTTP response
                if (!_config.Response.HasStarted)
                {
                    await _config.Response.CompleteAsync().ConfigureAwait(false);
                }

                // Remove the client from the channel manager
                await _channelService.UnRegisterClientAsync(_config.UniqueRequestId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during disposal of client {UniqueRequestId} {name}", _config.UniqueRequestId, _config.SMChannel.Name);
            }
        }
    }
}