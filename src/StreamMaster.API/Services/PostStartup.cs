using StreamMaster.Application.Services;
using StreamMaster.Infrastructure.EF;
using StreamMaster.Infrastructure.EF.PGSQL;

namespace StreamMaster.API.Services;

public class PostStartup(
    ILogger<PostStartup> logger,
    IServiceProvider serviceProvider,
    IBackgroundTaskQueue taskQueue,
    IHostEnvironment hostEnvironment) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        logger.LogInformation("Stream Master is starting.");

        using IServiceScope scope = serviceProvider.CreateScope();

        var repositoryContextInitializer = scope.ServiceProvider.GetRequiredService<RepositoryContextInitializer>();
        await repositoryContextInitializer.EnsureDefaultData().ConfigureAwait(false);

        if (hostEnvironment.IsDevelopment())
        {
            repositoryContextInitializer.CreateApplicationDirecotries();
        }

        //DirectoryHelper.EmptyDirectory(BuildInfo.HLSOutputFolder);

        PGSQLRepositoryContext repositoryContext = scope.ServiceProvider.GetRequiredService<PGSQLRepositoryContext>();
        IDataRefreshService dataRefreshService = scope.ServiceProvider.GetRequiredService<IDataRefreshService>();

        IStreamGroupService StreamGroupService = scope.ServiceProvider.GetRequiredService<IStreamGroupService>();
        await StreamGroupService.GetDefaultSGIdAsync();

        await repositoryContext.MigrateDatabaseAsync();

        await taskQueue.EPGSync(cancellationToken).ConfigureAwait(false);

        await taskQueue.ScanForTvLogos(cancellationToken).ConfigureAwait(false);

        await taskQueue.ScanDirectoryForEPGFiles(cancellationToken).ConfigureAwait(false);

        await taskQueue.ScanForCustomPlayLists(cancellationToken).ConfigureAwait(false);

        await taskQueue.ScanDirectoryForM3UFiles(cancellationToken).ConfigureAwait(false);

        await taskQueue.CacheChannelLogos(cancellationToken).ConfigureAwait(false);

        await taskQueue.CacheStreamLogos(cancellationToken).ConfigureAwait(false);

        await taskQueue.CreateSTRMFiles(cancellationToken).ConfigureAwait(false);

        while (taskQueue.HasJobs())
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        await taskQueue.CacheEPGChannelLogos(cancellationToken).ConfigureAwait(false);

        while (taskQueue.HasJobs())
        {
            await Task.Delay(50, cancellationToken).ConfigureAwait(false);
        }

        await taskQueue.SetIsSystemReady(true, cancellationToken).ConfigureAwait(false);
    }
}