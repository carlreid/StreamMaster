namespace StreamMaster.Application.Logos.Commands
{
    public record CacheEPGChannelLogosRequest() : IRequest<DataResponse<bool>>, IBaseRequest;

    public class CacheEPGChannelLogosRequestHandler(ILogoService logoService) : IRequestHandler<CacheEPGChannelLogosRequest, DataResponse<bool>>
    {
        public async Task<DataResponse<bool>> Handle(CacheEPGChannelLogosRequest request, CancellationToken cancellationToken)
        {
            DataResponse<bool> dataResponse = await logoService.CacheEPGChannelLogosAsync(cancellationToken);
            return DataResponse.True;
        }
    }
}