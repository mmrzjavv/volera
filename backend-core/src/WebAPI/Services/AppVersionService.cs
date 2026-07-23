using Core.Domain.Entities;
using Core.Domain.Interfaces;

namespace WebAPI.Services;

public class AppVersionService : IAppVersionService
{
    private readonly IAppSettingRepository _appSettingRepository;
    private readonly IConfiguration _configuration;

    public AppVersionService(IAppSettingRepository appSettingRepository, IConfiguration configuration)
    {
        _appSettingRepository = appSettingRepository;
        _configuration = configuration;
    }

    public async Task<string> GetVersionAsync(CancellationToken cancellationToken = default)
    {
        var setting = await _appSettingRepository.GetByKeyAsync(AppSettingKeys.AppVersion, cancellationToken);
        if (setting != null && !string.IsNullOrWhiteSpace(setting.Value))
            return setting.Value.Trim();
        return _configuration["AppVersion"] ?? "1.0.0";
    }

    public async Task SetVersionAsync(string version, CancellationToken cancellationToken = default)
    {
        var v = (version ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(v)) v = "1.0.0";
        await _appSettingRepository.SetAsync(AppSettingKeys.AppVersion, v, cancellationToken);
    }
}
