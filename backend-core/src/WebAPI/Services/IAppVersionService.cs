namespace WebAPI.Services;

public interface IAppVersionService
{
    Task<string> GetVersionAsync(CancellationToken cancellationToken = default);
    Task SetVersionAsync(string version, CancellationToken cancellationToken = default);
}
