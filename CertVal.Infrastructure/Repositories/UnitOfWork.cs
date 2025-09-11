using CertVal.Core.Events;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Events;
using Microsoft.EntityFrameworkCore.Storage;

namespace CertVal.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private IDbContextTransaction? _transaction;

    // Lazy initialization of repositories
    private IUserRepository? _users;
    private IWorkspaceRepository? _workspaces;
    private ICertificateRepository? _certificates;
    private INotificationRuleRepository? _notificationRules;
    private INotificationHistoryRepository? _notificationHistory;
    private IWorkspaceMemberRepository? _workspaceMembers;
    private IApiTokenRepository? _apiTokens;
    private IEventStoreRepository? _eventStore;

    public UnitOfWork(ApplicationDbContext context, IDomainEventDispatcher? domainEventDispatcher = null)
    {
        _context = context;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public IUserRepository Users =>
        _users ??= new UserRepository(_context);

    public IWorkspaceRepository Workspaces =>
        _workspaces ??= new WorkspaceRepository(_context);

    public ICertificateRepository Certificates =>
        _certificates ??= new CertificateRepository(_context);

    public INotificationRuleRepository NotificationRules =>
        _notificationRules ??= new NotificationRuleRepository(_context);

    public INotificationHistoryRepository NotificationHistory =>
        _notificationHistory ??= new NotificationHistoryRepository(_context);

    public IWorkspaceMemberRepository WorkspaceMembers =>
        _workspaceMembers ??= new WorkspaceMemberRepository(_context);

    public IApiTokenRepository ApiTokens =>
        _apiTokens ??= new ApiTokenRepository(_context);

    public IEventStoreRepository EventStore =>
        _eventStore ??= new EventStoreRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var domainEvents = CollectDomainEvents();

        var result = await _context.SaveChangesAsync(cancellationToken);

        if (_domainEventDispatcher != null && domainEvents.Any())
        {
            await _domainEventDispatcher.PublishAsync(domainEvents, cancellationToken);
        }

        return result;
    }

    private List<DomainEvent> CollectDomainEvents()
    {
        var domainEvents = new List<DomainEvent>();

        var entitiesWithEvents = _context.ChangeTracker
            .Entries<IHasDomainEvents>()
            .Where(e => e.Entity.DomainEvents.Any())
            .ToList();

        foreach (var entity in entitiesWithEvents)
        {
            domainEvents.AddRange(entity.Entity.DomainEvents);
            entity.Entity.ClearDomainEvents();
        }

        return domainEvents;
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            await SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to rollback");
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}