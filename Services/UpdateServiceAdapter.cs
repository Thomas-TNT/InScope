using System.Threading;
using System.Threading.Tasks;

namespace InScope.Services;

/// <summary>
/// Adapter that implements IUpdateService by delegating to UpdateService static methods.
/// </summary>
public sealed class UpdateServiceAdapter : IUpdateService
{
    public string GetCurrentVersion() => UpdateService.GetCurrentVersion();

    public Task<UpdateService.UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default) =>
        UpdateService.CheckForUpdateAsync(cancellationToken);

    public Task<string> DownloadInstallerAsync(string downloadUrl, IProgress<long>? progress = null, CancellationToken cancellationToken = default) =>
        UpdateService.DownloadInstallerAsync(downloadUrl, progress, cancellationToken);
}
