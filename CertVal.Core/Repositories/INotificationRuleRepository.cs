using CertVal.Core.Entities;

namespace CertVal.Core.Repositories;

public interface INotificationRuleRepository : IRepository<NotificationRule>
{
    Task<IEnumerable<NotificationRule>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NotificationRule>> GetRulesForCertificateExpiryAsync(DateTime expiryDate, CancellationToken cancellationToken = default);
}
