using Core.Domain.Entities;

namespace Core.Domain.Interfaces;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string value, CancellationToken cancellationToken = default);
}
