﻿using Microsoft.Extensions.Logging;

using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Extensions;

namespace StreamMaster.Infrastructure.Services.Frontend.Mappers
{
    public class FaviconMapper(ILogger<FaviconMapper> logger, IOptionsMonitor<Setting> intSettings) : StaticResourceMapperBase(logger)
    {
        private readonly Setting settings = intSettings.CurrentValue;

        public override bool CanHandle(string resourceUrl)
        {
            return resourceUrl.EndsWithIgnoreCase("/favicon.ico");
        }

        public override Task<string> MapAsync(string resourceUrl)
        {
            return Task.FromResult(Path.Combine(BuildInfo.StartUpPath, settings.UiFolder, "favicon.ico"));
        }
    }
}