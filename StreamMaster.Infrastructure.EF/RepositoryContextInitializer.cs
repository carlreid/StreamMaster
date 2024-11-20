﻿using Microsoft.EntityFrameworkCore;

using StreamMaster.Domain.Configuration;
using StreamMaster.Domain.Helpers;
using StreamMaster.Infrastructure.EF.PGSQL;

namespace StreamMaster.Infrastructure.EF;

public class RepositoryContextInitializer(ILogger<RepositoryContextInitializer> logger, PGSQLRepositoryContext context, IOptionsMonitor<Setting> intSettings)
{
    public async Task InitializeAsync()
    {
        try
        {
            string? currentMigration = context.Database.GetAppliedMigrations().LastOrDefault();
            if (currentMigration is not null and "20241017230528_M3U8OutPutProfile")
            {
                context.Database.ExecuteSqlRaw("DELETE FROM public.\"__EFMigrationsHistory\";");
                context.Database.ExecuteSqlRaw(
       "INSERT INTO public.\"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") " +
       "VALUES ('20241018224015_Initial', '8.0.10');"
   );
            }

            await context.Database.MigrateAsync().ConfigureAwait(false);

            Setting settings = intSettings.CurrentValue;

            if (!context.StreamGroups.Any(a => a.Name == BuildInfo.DefaultStreamGroupName))
            {
                StreamGroup sg = new() { Name = BuildInfo.DefaultStreamGroupName, IsReadOnly = true, IsSystem = true, DeviceID = settings.DeviceID, GroupKey = Guid.NewGuid().ToString().Replace("-", "") };
                _ = context.Add(sg);
                StreamGroupProfile profile = new()
                {
                    ProfileName = "Default",
                    OutputProfileName = "Default",
                    CommandProfileName = settings.DefaultCommandProfileName
                };

                _ = context.StreamGroupProfiles.Add(profile);
                sg.StreamGroupProfiles.Add(profile);

                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (!context.ChannelGroups.Any(a => a.Name == "Dummy"))
            {
                _ = context.Add(new ChannelGroup { Name = "Dummy", IsReadOnly = true, IsSystem = true });
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (!context.ChannelGroups.Any(a => a.Name == "CustomPlayList"))
            {
                _ = context.Add(new ChannelGroup { Name = "CustomPlayList", IsReadOnly = true, IsSystem = true });
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (!context.ChannelGroups.Any(a => a.Name == "SystemMessages"))
            {
                _ = context.Add(new ChannelGroup { Name = "SystemMessages", IsReadOnly = true, IsSystem = true });
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }

            if (!context.ChannelGroups.Any(a => a.Name == "Intros"))
            {
                _ = context.Add(new ChannelGroup { Name = "Intros", IsReadOnly = true, IsSystem = true });
                _ = await context.SaveChangesAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database.");
            throw;
        }
    }

    public void TrySeed()
    {
        DirectoryHelper.CreateApplicationDirectories();
    }
}