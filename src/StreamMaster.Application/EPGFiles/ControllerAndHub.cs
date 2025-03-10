using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StreamMaster.Application.EPGFiles.Commands;
using StreamMaster.Application.EPGFiles.Queries;

namespace StreamMaster.Application.EPGFiles.Controllers
{
    [Authorize]
    public partial class EPGFilesController(ILogger<EPGFilesController> _logger) : ApiControllerBase, IEPGFilesController
    {
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<string>>> GetEPGFileNames()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGFileNamesRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetEPGFileNames.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetEPGFileNames.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<EPGFilePreviewDto>>> GetEPGFilePreviewById([FromQuery] GetEPGFilePreviewByIdRequest request)
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetEPGFilePreviewById.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetEPGFilePreviewById.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<EPGFileDto>>> GetEPGFiles()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGFilesRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetEPGFiles.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetEPGFiles.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<int>> GetEPGNextEPGNumber()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGNextEPGNumberRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetEPGNextEPGNumber.", statusCode: 500) : Ok(ret.Data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetEPGNextEPGNumber.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<PagedResponse<EPGFileDto>>> GetPagedEPGFiles([FromQuery] QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedEPGFilesRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> CreateEPGFile(CreateEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> DeleteEPGFile(DeleteEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> ProcessEPGFile(ProcessEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> RefreshEPGFile(RefreshEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> UpdateEPGFile(UpdateEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
    }
}

namespace StreamMaster.Application.Hubs
{
    public partial class StreamMasterHub : IEPGFilesHub
    {
        public async Task<List<string>> GetEPGFileNames()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGFileNamesRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<List<EPGFilePreviewDto>> GetEPGFilePreviewById(GetEPGFilePreviewByIdRequest request)
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<List<EPGFileDto>> GetEPGFiles()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGFilesRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<int> GetEPGNextEPGNumber()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetEPGNextEPGNumberRequest())).ConfigureAwait(false);
            return ret.Data;
        }
        public async Task<PagedResponse<EPGFileDto>> GetPagedEPGFiles(QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedEPGFilesRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        public async Task<APIResponse?> CreateEPGFile(CreateEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> DeleteEPGFile(DeleteEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> ProcessEPGFile(ProcessEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> RefreshEPGFile(RefreshEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> UpdateEPGFile(UpdateEPGFileRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
    }
}
