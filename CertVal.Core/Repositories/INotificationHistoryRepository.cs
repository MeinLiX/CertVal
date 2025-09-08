using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface INotificationHistoryRepository : IRepository<NotificationHistory>
{
    Task<IEnumerable<NotificationHistory>> GetPendingAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationHistory>> GetFailedAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationHistory>> GetByCertificateAsync(Guid certificateId, CancellationToken cancellationToken = default);
    Task<NotificationHistory?> GetLastNotificationAsync(Guid certificateId, Guid ruleId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid notificationId, CancellationToken cancellationToken = default);
}
