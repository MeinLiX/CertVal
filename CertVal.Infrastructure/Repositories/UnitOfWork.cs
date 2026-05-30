using CertVal.Core.Events;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CertVal.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private readonly IDomainEventDispatcher? _domainEventDispatcher;
    private IDbContextTransaction? _transaction;
    private readonly Lock _lock = new();
    private bool _disposed = false;

    private readonly Lazy<IUserRepository> _users;
    private readonly Lazy<IWorkspaceRepository> _workspaces;
    private readonly Lazy<ICertificateRepository> _certificates;
    private readonly Lazy<INotificationRuleRepository> _notificationRules;
    private readonly Lazy<INotificationHistoryRepository> _notificationHistory;
    private readonly Lazy<IWorkspaceMemberRepository> _workspaceMembers;
    private readonly Lazy<IApiTokenRepository> _apiTokens;
    private readonly Lazy<IRefreshTokenRepository> _refreshTokens;
    private readonly Lazy<IEventStoreRepository> _eventStore;

    public UnitOfWork(ApplicationDbContext context, IDomainEventDispatcher? domainEventDispatcher = null)
    {
        _context = context;
        _domainEventDispatcher = domainEventDispatcher;

        _users = new Lazy<IUserRepository>(() => new UserRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _workspaces = new Lazy<IWorkspaceRepository>(() => new WorkspaceRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _certificates = new Lazy<ICertificateRepository>(() => new CertificateRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _notificationRules = new Lazy<INotificationRuleRepository>(() => new NotificationRuleRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _notificationHistory = new Lazy<INotificationHistoryRepository>(() => new NotificationHistoryRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _workspaceMembers = new Lazy<IWorkspaceMemberRepository>(() => new WorkspaceMemberRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _apiTokens = new Lazy<IApiTokenRepository>(() => new ApiTokenRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _refreshTokens = new Lazy<IRefreshTokenRepository>(() => new RefreshTokenRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
        _eventStore = new Lazy<IEventStoreRepository>(() => new EventStoreRepository(_context), LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public IUserRepository Users => _users.Value;
    public IWorkspaceRepository Workspaces => _workspaces.Value;
    public ICertificateRepository Certificates => _certificates.Value;
    public INotificationRuleRepository NotificationRules => _notificationRules.Value;
    public INotificationHistoryRepository NotificationHistory => _notificationHistory.Value;
    public IWorkspaceMemberRepository WorkspaceMembers => _workspaceMembers.Value;
    public IApiTokenRepository ApiTokens => _apiTokens.Value;
    public IRefreshTokenRepository RefreshTokens => _refreshTokens.Value;
    public IEventStoreRepository EventStore => _eventStore.Value;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(UnitOfWork)));

        List<DomainEvent> domainEvents;
        int result;

        lock (_lock)
        {
            domainEvents = CollectDomainEvents();
        }

        result = await _context.SaveChangesAsync(cancellationToken);

        if (_domainEventDispatcher != null && domainEvents.Any())
        {
            try
            {
                await _domainEventDispatcher.PublishAsync(domainEvents, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error publishing domain events: {ex.Message}");
            }
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
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(UnitOfWork)));

        lock (_lock)
        {
            if (_transaction != null)
            {
                throw new InvalidOperationException("Transaction already started");
            }
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(UnitOfWork)));
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
            lock (_lock)
            {
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, new ObjectDisposedException(nameof(UnitOfWork)));
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
            lock (_lock)
            {
                _transaction = null;
            }
        }
    }

    public async Task ExecuteInTransactionAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        var strategy = _context.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                await operation();

                await _context.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _context.Dispose();
        _disposed = true;
    }
}