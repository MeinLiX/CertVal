namespace CertVal.Application.Common.Interfaces;

public interface IRateLimitService
{
    /// <summary>
    /// Checks if the action is allowed. If allowed, sets the rate limit key.
    /// </summary>
    /// <param name="key">The unique key for the action</param>
    /// <param name="duration">The duration to block subsequent requests</param>
    /// <returns>True if allowed, False if rate limited</returns>
    Task<bool> IsAllowedAsync(string key, TimeSpan duration);
}
