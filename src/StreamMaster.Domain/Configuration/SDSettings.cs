﻿namespace StreamMaster.Domain.Configuration
{
    [TsInterface(AutoI = false, IncludeNamespace = false, FlattenHierarchy = true, AutoExportMethods = false)]
    public class SDSettings
    {
        public int MaxSubscribedLineups { get; set; } = 4;
        public bool AlternateSEFormat { get; set; } = false;
        public string AlternateLogoStyle { get; set; } = "WHITE";
        public DateTime TokenErrorTimestamp { get; set; }

        public string ArtworkSize { get; set; } = "Lg";
        public bool ExcludeCastAndCrew { get; set; } = false;
        public string PreferredLogoStyle { get; set; } = "DARK";

        public bool PrefixEpisodeTitle { get; set; } = true; // Not used
        public bool AppendEpisodeDesc { get; set; } = true; // Append Session and Episode to Program Description
        public bool PrefixEpisodeDescription { get; set; } = false; // Prefix Session and Episode to Program Description
        public bool EpisodeAppendProgramDescription { get; set; } = false; // Append Program Description to Session Description

        public bool SDEnabled { get; set; } = false;
        public int SDEPGDays { get; set; } = 7;
        public string SDCountry { get; set; } = "USA";
        public string SDPassword { get; set; } = string.Empty;
        public string SDPostalCode { get; set; } = string.Empty;
        public List<StationIdLineup> SDStationIds { get; set; } = [];
        public List<HeadendToView> HeadendsToView { get; set; } = [];
        public string SDUserName { get; set; } = string.Empty;
        public string UserAgent { get; set; } = "StreamMaster/0.0.0.Sha.0000000000000000000000000000000000000000";
        public bool SeriesPosterArt { get; set; } = false;

        public bool MovieImages { get; set; } = true;
        public bool SeasonImages { get; set; } = false;
        public bool SeriesImages { get; set; } = false;
        public bool SportsImages { get; set; } = false;
        public bool EpisodeImages { get; set; } = true;

        public string SeriesPosterAspect { get; set; } = "4x3";

        public string MoviePosterAspect { get; set; } = "2x3";

        public bool XmltvAddFillerData { get; set; } = true;

        public int XmltvFillerProgramLength { get; set; } = 4;

        public bool XmltvExtendedInfoInTitleDescriptions { get; set; } = false;
        public bool XmltvIncludeChannelNumbers { get; set; } = false;
        public bool XmltvSingleImage { get; set; } = false;
        public DateTime SDTooManyRequestsSuspend { get; set; } = DateTime.MinValue;
    }
}