using StreamMaster.Domain.EPG;
using StreamMaster.Domain.Helpers;
using StreamMaster.Domain.Models;

using StreamMaster.Domain.XmltvXml;
using StreamMaster.Streams.Domain.Interfaces;

namespace StreamMaster.EPG;

/// <summary>
/// An implementation of IEpgMatcher that attempts to find the best EPG station match.
/// </summary>
public sealed class FuzzyEpgMatcher(ICacheManager cacheManager) : IEpgMatcher
{
    /// <inheritdoc/>
    public async Task<StationChannelName?> MatchAsync(SMChannel channel, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // If EPGID is not dummy, try to find a direct match
        if (EPGHelper.TryExtractEPGNumberAndStationId(channel.EPGId, out int epgNumber, out string stationId))
        {
            if (cacheManager.StationChannelNames.TryGetValue(epgNumber, out List<StationChannelName>? channelList))
            {
                // Exact match by channel/stationId
                StationChannelName? exactMatch = channelList.FirstOrDefault(c => c.Channel.Equals(stationId, StringComparison.OrdinalIgnoreCase));
                if (exactMatch is not null)
                {
                    return exactMatch;
                }

                // If exact match not found, we still try fuzzy match by TVGName or Name
                StationChannelName? bestFuzzy = await FindBestFuzzyMatchAsync(channel, channelList, cancellationToken).ConfigureAwait(false);
                if (bestFuzzy is not null)
                {
                    return bestFuzzy;
                }
            }
        }

        // If we cannot find by EPGID (or it's dummy), try all channels in all EPGs
        List<StationChannelName> allChannels = [.. cacheManager.StationChannelNames.Values.SelectMany(v => v)];
        StationChannelName? bestOverall = await FindBestFuzzyMatchAsync(channel, allChannels, cancellationToken).ConfigureAwait(false);
        return bestOverall;
    }

    /// <summary>
    /// Attempts a fuzzy match using the TVGName and possibly the SMStream Name against a collection of channels.
    /// </summary>
    /// <param name="channel">The SMStream to match.</param>
    /// <param name="candidates">The candidates to match against.</param>
    /// <param name="cancellationToken">Token to cancel operation.</param>
    /// <returns>The best fuzzy match, or null if none found.</returns>
    private static Task<StationChannelName?> FindBestFuzzyMatchAsync(SMChannel channel, IEnumerable<StationChannelName> candidates, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            string tvgName = channel.TVGName ?? string.Empty;
            string fallbackName = channel.Name ?? string.Empty;

            // We try matching against multiple fields. We'll consider DisplayName and ChannelName.
            // We'll score each candidate, pick the lowest Levenshtein distance.
            // If TVGName is empty, we fallback to Name.
            if (string.IsNullOrEmpty(tvgName))
            {
                tvgName = fallbackName;
            }

            tvgName = tvgName.Trim();
            if (string.IsNullOrEmpty(tvgName))
            {
                return null;
            }

            // Compute fuzzy scores
            List<(StationChannelName Channel, int Score)> scored = [.. candidates
                .Select(c =>
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    // Consider multiple fields. You can weight them differently if desired.
                    // For simplicity, we take the minimum distance of TVGName to any of the fields.
                    int scoreDisplayName = FuzzyMatcher.Score(tvgName, c.DisplayName);
                    int scoreChannelName = FuzzyMatcher.Score(tvgName, c.ChannelName);
                    int scoreChannel = FuzzyMatcher.Score(tvgName, c.Channel);

                    int bestScore = Math.Min(scoreDisplayName, Math.Min(scoreChannelName, scoreChannel));

                    return (Channel: c, Score: bestScore);
                })
                .Where(x => x.Score < int.MaxValue)
                .OrderBy(x => x.Score)];

            // Take best match if any
            return scored.Count > 0 ? scored[0].Channel : null;
        }, cancellationToken);
    }
}