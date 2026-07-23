using Core.Domain.Entities;
using Core.Domain.Interfaces;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly ApplicationDbContext _context;

    public AppSettingRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppSetting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
    }

    public async Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
    {
        var existing = await _context.AppSettings.FirstOrDefaultAsync(s => s.Key == key, cancellationToken);
        if (existing != null)
        {
            existing.SetValue(value);
        }
        else
        {
            _context.AppSettings.Add(new AppSetting(key, value));
        }
        await _context.SaveChangesAsync(cancellationToken);
    }
}
