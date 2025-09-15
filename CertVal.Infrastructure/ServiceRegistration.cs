using CertVal.Application.Common.Interfaces;
using CertVal.Core.Events;
using CertVal.Core.Messaging;
using CertVal.Core.Repositories;
using CertVal.Infrastructure.Authentication;
using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Events;
using CertVal.Infrastructure.Messaging;
using CertVal.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CertVal.Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        AddDomainEventHandlers(services);
        AddRepositories(services);
        services.AddMessaging();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static void AddDomainEventHandlers(IServiceCollection services)
    {
        // User event handlers
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>, UserEventHandlers>();
        services.AddScoped<IDomainEventHandler<UserEmailConfirmedEvent>, UserEventHandlers>();
        services.AddScoped<IDomainEventHandler<UserPasswordChangedEvent>, UserEventHandlers>();

        // Workspace event handlers
        services.AddScoped<IDomainEventHandler<WorkspaceCreatedEvent>, WorkspaceEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceUpdatedEvent>, WorkspaceEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceMemberInvitedEvent>, WorkspaceEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceMemberJoinedEvent>, WorkspaceEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceMemberRemovedEvent>, WorkspaceEventHandlers>();

        // Certificate event handlers
        services.AddScoped<IDomainEventHandler<CertificateUploadedEvent>, CertificateEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateExpiringEvent>, CertificateEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateExpiredEvent>, CertificateEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateBundleProcessedEvent>, CertificateEventHandlers>();

        // Notification event handlers
        services.AddScoped<IDomainEventHandler<NotificationRuleCreatedEvent>, NotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<NotificationSentEvent>, NotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<NotificationFailedEvent>, NotificationEventHandlers>();

        // API token event handlers
        services.AddScoped<IDomainEventHandler<ApiTokenCreatedEvent>, ApiTokenEventHandlers>();
        services.AddScoped<IDomainEventHandler<ApiTokenUsedEvent>, ApiTokenEventHandlers>();
        services.AddScoped<IDomainEventHandler<ApiTokenRevokedEvent>, ApiTokenEventHandlers>();

        // Email notification event handlers
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>, EmailNotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceMemberInvitedEvent>, EmailNotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateExpiringEvent>, EmailNotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateExpiredEvent>, EmailNotificationEventHandlers>();
    }

    private static void AddRepositories(IServiceCollection services)
    {
        services.AddScoped<IEventStoreRepository, EventStoreRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkspaceRepository, WorkspaceRepository>();
        services.AddScoped<ICertificateRepository, CertificateRepository>();
        services.AddScoped<INotificationRuleRepository, NotificationRuleRepository>();
        services.AddScoped<INotificationHistoryRepository, NotificationHistoryRepository>();
        services.AddScoped<IWorkspaceMemberRepository, WorkspaceMemberRepository>();
        services.AddScoped<IApiTokenRepository, ApiTokenRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        var contextOptions = scope.ServiceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>();
        using var context = new ApplicationDbContext(contextOptions);

        try
        {
            logger.LogInformation("Starting database migration...");
            await context.Database.MigrateAsync();
            logger.LogInformation("Database migration completed successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization failed: {Message}", ex.Message);
            throw;
        }
    }

    public static IServiceCollection AddMessaging(this IServiceCollection services)
    {
        // Add messaging configuration from Core
        services.AddOptions<MessagingConfiguration>()
            .BindConfiguration(MessagingConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register the RabbitMQ implementation
        services.AddSingleton<IEmailNotificationPublisher, RabbitMqEmailNotificationPublisher>();

        return services;
    }
}