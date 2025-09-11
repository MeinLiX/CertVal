namespace CertVal.Core.Repositories;

public interface IUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IWorkspaceRepository Workspaces { get; }
    ICertificateRepository Certificates { get; }
    INotificationRuleRepository NotificationRules { get; }
    INotificationHistoryRepository NotificationHistory { get; }
    IWorkspaceMemberRepository WorkspaceMembers { get; }
    IApiTokenRepository ApiTokens { get; }
    IEventStoreRepository EventStore { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}