﻿using StreamMaster.PlayList.Models;

namespace StreamMaster.PlayList;

public interface ICustomPlayListBuilder
{
    CustomPlayList? GetCustomForFilePlayList(string Name);
    //List<XmltvProgramme> GetXmltvProgrammeForPeriod(StationChannelName stationChannelName, DateTime startDate, int days);
    List<(Movie Movie, DateTime StartTime, DateTime EndTime)> GetMoviesForPeriod(string customPlayListName, DateTime startDate, int days);
    (CustomStreamNfo StreamNfo, int SecondsIn) GetCurrentVideoAndElapsedSeconds(string customPlayListName);
    List<CustomPlayList> GetCustomPlayLists();
    CustomPlayList? GetCustomPlayList(string Name);
    (CustomPlayList? customPlayList, CustomStreamNfo? customStreamNfo) GetCustomPlayListByMovieId(string movieId);
    string? GetCustomPlayListLogoFromFileName(string FileName);
}