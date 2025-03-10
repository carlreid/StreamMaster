using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using StreamMaster.Application.ChannelGroups.Commands;
using StreamMaster.Application.ChannelGroups.Queries;

namespace StreamMaster.Application.ChannelGroups.Controllers
{
    [Authorize]
    public partial class ChannelGroupsController(ILogger<ChannelGroupsController> _logger) : ApiControllerBase, IChannelGroupsController
    {
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<ChannelGroupDto>>> GetChannelGroupsFromSMChannels()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetChannelGroupsFromSMChannelsRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetChannelGroupsFromSMChannels.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetChannelGroupsFromSMChannels.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<List<ChannelGroupDto>>> GetChannelGroups()
        {
            try
            {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetChannelGroupsRequest())).ConfigureAwait(false);
             return ret.IsError ? Problem(detail: "An unexpected error occurred retrieving GetChannelGroups.", statusCode: 500) : Ok(ret.Data?? []);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while processing the request to get GetChannelGroups.");
                return Problem(detail: "An unexpected error occurred. Please try again later.", statusCode: 500);
            }
        }
        [HttpGet]
        [Route("[action]")]
        public async Task<ActionResult<PagedResponse<ChannelGroupDto>>> GetPagedChannelGroups([FromQuery] QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedChannelGroupsRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        [HttpPost]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> CreateChannelGroup(CreateChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> DeleteAllChannelGroupsFromParameters(DeleteAllChannelGroupsFromParametersRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> DeleteChannelGroup(DeleteChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpDelete]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> DeleteChannelGroups(DeleteChannelGroupsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> UpdateChannelGroup(UpdateChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
        [HttpPatch]
        [Route("[action]")]
        public async Task<ActionResult<APIResponse?>> UpdateChannelGroups(UpdateChannelGroupsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret == null ? NotFound(ret) : Ok(ret);
        }
    }
}

namespace StreamMaster.Application.Hubs
{
    public partial class StreamMasterHub : IChannelGroupsHub
    {
        public async Task<List<ChannelGroupDto>> GetChannelGroupsFromSMChannels()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetChannelGroupsFromSMChannelsRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<List<ChannelGroupDto>> GetChannelGroups()
        {
             var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetChannelGroupsRequest())).ConfigureAwait(false);
            return ret.Data?? [];
        }
        public async Task<PagedResponse<ChannelGroupDto>> GetPagedChannelGroups(QueryStringParameters Parameters)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(new GetPagedChannelGroupsRequest(Parameters))).ConfigureAwait(false);
            return ret?? new();
        }
        public async Task<APIResponse?> CreateChannelGroup(CreateChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> DeleteAllChannelGroupsFromParameters(DeleteAllChannelGroupsFromParametersRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> DeleteChannelGroup(DeleteChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> DeleteChannelGroups(DeleteChannelGroupsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> UpdateChannelGroup(UpdateChannelGroupRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
        public async Task<APIResponse?> UpdateChannelGroups(UpdateChannelGroupsRequest request)
        {
            var ret = await APIStatsLogger.DebugAPI(Sender.Send(request)).ConfigureAwait(false);
            return ret;
        }
    }
}
