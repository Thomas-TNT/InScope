using System.Threading;
using System.Threading.Tasks;

namespace InScope.Services;

public interface IUpdateService
{
    string GetCurrentVersion();
    Task<UpdateService.UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default);
    Task<string> DownloadInstallerAsync(string downloadUrl, IProgress<long>? progress = null, CancellationToken cancellationToken = default);
}
