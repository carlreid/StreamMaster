using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StreamMaster.Application.M3UFiles.Commands;
using StreamMaster.Application.M3UFiles.Queries;

namespace StreamMaster.Application.M3UFiles.Controllers
{
    [Authorize]
    public partial class M3UFilesController(ILogger<M3UFilesController> _logger) : ApiControllerBase, IM3UFilesController
    {
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<string>>> GetM3UFileNames()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetM3UFileNamesRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetM3UFileNames.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetM3UFileNames.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<M3UFileDto>>> GetM3UFiles()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetM3UFilesRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetM3UFiles.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetM3UFiles.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<PagedResponse<M3UFileDto>>> GetPagedM3UFiles([FromQuery] QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedM3UFilesRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> CreateM3UFile(CreateM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> DeleteM3UFile(DeleteM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> ProcessM3UFile(ProcessM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> RefreshM3UFile(RefreshM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> SyncChannels(SyncChannelsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> UpdateM3UFile(UpdateM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
    }
}

namespace StreamMaster.Application.Hubs
{
    public partial class StreamMasterHub : IM3UFilesHub
    {
        public async Task<List<string>> GetM3UFileNames()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetM3UFileNamesRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<List<M3UFileDto>> GetM3UFiles()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetM3UFilesRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<PagedResponse<M3UFileDto>> GetPagedM3UFiles(QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedM3UFilesRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        public async Task<APIResponse?> CreateM3UFile(CreateM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> DeleteM3UFile(DeleteM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> ProcessM3UFile(ProcessM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> RefreshM3UFile(RefreshM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> SyncChannels(SyncChannelsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> UpdateM3UFile(UpdateM3UFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
    }
}
