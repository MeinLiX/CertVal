using System.Collections.Concurrent;

namespace CertVal.EmailService.Services;

public sealed class IdempotencyTracker : IDisposable
{
    // redis
    private readonly ConcurrentDictionary<string, DateTime> _processedMessages = new();
    private readonly TimeSpan _retentionPeriod;
    private readonly Timer _cleanupTimer;
    private readonly ILogger<IdempotencyTracker> _logger;

    public IdempotencyTracker(ILogger<IdempotencyTracker> logger, TimeSpan? retentionPeriod = null)
    {
        _logger = logger;
        _retentionPeriod = retentionPeriod ?? TimeSpan.FromHours(24);
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }

    public bool TryMarkAsProcessed(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return false;

        var now = DateTime.UtcNow;
        var added = _processedMessages.TryAdd(messageId, now);

        if (!added)
        {
            _logger.LogWarning("Duplicate message detected: {MessageId}", messageId);
        }

        return added;
    }

    public bool IsProcessed(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return false;

        return _processedMessages.ContainsKey(messageId);
    }

    public bool RemoveProcessed(string messageId)
    {
        return _processedMessages.TryRemove(messageId, out _);
    }

    private void CleanupExpiredEntries(object? state)
    {
        try
        {
            var now = DateTime.UtcNow;
            var expiredKeys = _processedMessages
                .Where(kvp => now - kvp.Value > _retentionPeriod)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _processedMessages.TryRemove(key, out _);
            }

            if (expiredKeys.Count > 0)
            {
                _logger.LogDebug("Cleaned up {Count} expired idempotency entries", expiredKeys.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during idempotency cleanup");
        }
    }

    public int GetTrackedMessageCount() => _processedMessages.Count;

    public void Dispose()
    {
        _cleanupTimer?.Dispose();
        _processedMessages.Clear();
    }
}
