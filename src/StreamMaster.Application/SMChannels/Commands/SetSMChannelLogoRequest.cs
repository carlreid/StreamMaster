﻿namespace StreamMaster.Application.SMChannels.Commands;

[SMAPI]
[TsInterface(AutoI = false, IncludeNamespace = false, FlattenHierarchy = true, AutoExportMethods = false)]
public record SetSMChannelLogoRequest(int SMChannelId, string Logo) : IRequest<APIResponse>;

internal class SetSMChannelLogoRequestHandler(IRepositoryWrapper Repository, IDataRefreshService dataRefreshService, IMessageService messageService) : IRequestHandler<SetSMChannelLogoRequest, APIResponse>
{
    public async Task<APIResponse> Handle(SetSMChannelLogoRequest request, CancellationToken cancellationToken)
    {
        APIResponse ret = await Repository.SMChannel.SetSMChannelLogo(request.SMChannelId, request.Logo).ConfigureAwait(false);
        if (ret.IsError)
        {
            await messageService.SendError($"Set logo failed {ret.Message}");
        }
        else
        {
            await dataRefreshService.RefreshLogos().ConfigureAwait(false);
            //await dataRefreshService.RefreshSMChannels().ConfigureAwait(false);
        }
        return ret;
    }
}
