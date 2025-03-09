namespace StreamMaster.Domain.Services
{
    public interface IEPGHelper
    {
        (int epgNumber, string stationId) ExtractEPGNumberAndStationId(string epgId);

        bool IsMovie(int epgNumber);

        bool IsSchedulesDirect(int epgNumber);
    }
}