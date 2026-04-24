using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ContosoDashboard.Services;

/// <summary>
/// Storage quota management implementation
/// </summary>
public class QuotaService : IQuotaService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<QuotaService> _logger;
    private readonly long _totalQuota;
    private readonly int _warningThreshold;

    public QuotaService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IConfiguration configuration,
        ILogger<QuotaService> logger)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _logger = logger;
        
        _totalQuota = configuration.GetValue<long>("DocumentSettings:TotalStorageQuota", 10737418240);
        _warningThreshold = configuration.GetValue<int>("DocumentSettings:QuotaWarningThreshold", 80);
    }

    public async Task<bool> CanUploadAsync(int userId, long fileSize)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for quota check", userId);
            return false;
        }

        var currentUsed = user.StorageUsed;
        var quota = user.StorageQuota;

        // Check if user has enough quota
        if (currentUsed + fileSize > quota)
        {
            _logger.LogWarning("User {UserId} quota exceeded. Used: {Used}, Quota: {Quota}, Requested: {Size}",
                userId, currentUsed, quota, fileSize);
            return false;
        }

        // Check total system quota
        if (await IsTotalQuotaExceededAsync(fileSize))
        {
            _logger.LogWarning("Total system quota would be exceeded. Requested: {Size}", fileSize);
            return false;
        }

        return true;
    }

    public async Task<bool> IsTotalQuotaExceededAsync(long fileSize)
    {
        var totalUsed = await GetTotalStorageUsedAsync();
        return totalUsed + fileSize > _totalQuota;
    }

    public async Task<long> GetUserStorageUsedAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.StorageUsed ?? 0;
    }

    public async Task<long> GetUserStorageQuotaAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user?.StorageQuota ?? 524288000; // Default 500 MB
    }

    public async Task<long> GetTotalStorageUsedAsync()
    {
        // Sum up all users' storage used
        return await _context.Users.SumAsync(static u => u.StorageUsed);
    }

    public Task<long> GetTotalStorageQuotaAsync()
    {
        return Task.FromResult(_totalQuota);
    }

    public async Task UpdateUserStorageAsync(int userId, long delta)
    {
        var user = await _context.Users.FindAsync(userId);
        
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found for storage update", userId);
            return;
        }

        user.StorageUsed += delta;
        
        if (user.StorageUsed < 0)
        {
            user.StorageUsed = 0;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} storage updated by {Delta} bytes. New used: {Used}",
            userId, delta, user.StorageUsed);

        // Check if we should warn about total quota
        await CheckTotalQuotaWarningAsync();
    }

    private async Task CheckTotalQuotaWarningAsync()
    {
        var totalUsed = await GetTotalStorageUsedAsync();
        var percentage = (int)((totalUsed * 100) / _totalQuota);

        if (percentage >= _warningThreshold)
        {
            _logger.LogWarning("Total storage usage at {Percentage}% ({Used}/{Total})",
                percentage, totalUsed, _totalQuota);
        }
    }
}