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
        services.AddSingleton<IBackgroundDomainEventDispatcher, BackgroundDomainEventDispatcher>();
        services.AddSingleton<IDomainEventDispatcher, HybridDomainEventDispatcher>();
        services.AddHostedService<BackgroundDomainEventDispatcher>(sp => (BackgroundDomainEventDispatcher)sp.GetRequiredService<IBackgroundDomainEventDispatcher>());

        AddDomainEventHandlers(services);
        AddRepositories(services);
        services.AddMessaging();

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        return services;
    }

    private static void AddDomainEventHandlers(IServiceCollection services)
    {
        // User event handlers
        services.AddScoped<UserEventHandlers>();
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>>(sp => sp.GetRequiredService<UserEventHandlers>());
        services.AddScoped<IDomainEventHandler<UserEmailConfirmedEvent>>(sp => sp.GetRequiredService<UserEventHandlers>());
        services.AddScoped<IDomainEventHandler<UserPasswordChangedEvent>>(sp => sp.GetRequiredService<UserEventHandlers>());

        // Workspace event handlers
        services.AddScoped<WorkspaceEventHandlers>();
        services.AddScoped<IDomainEventHandler<WorkspaceCreatedEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());
        services.AddScoped<IDomainEventHandler<WorkspaceUpdatedEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());
        services.AddScoped<IDomainEventHandler<WorkspaceOwnershipTransferredEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());
        services.AddScoped<IDomainEventHandler<WorkspaceMemberInvitedEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());
        services.AddScoped<IDomainEventHandler<WorkspaceMemberJoinedEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());
        services.AddScoped<IDomainEventHandler<WorkspaceMemberRemovedEvent>>(sp => sp.GetRequiredService<WorkspaceEventHandlers>());

        // Certificate event handlers
        services.AddScoped<CertificateEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateUploadedEvent>>(sp => sp.GetRequiredService<CertificateEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateExpiringEvent>>(sp => sp.GetRequiredService<CertificateEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateExpiredEvent>>(sp => sp.GetRequiredService<CertificateEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateBundleProcessedEvent>>(sp => sp.GetRequiredService<CertificateEventHandlers>());

        // Notification event handlers
        services.AddScoped<NotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<NotificationRuleCreatedEvent>>(sp => sp.GetRequiredService<NotificationEventHandlers>());
        services.AddScoped<IDomainEventHandler<NotificationSentEvent>>(sp => sp.GetRequiredService<NotificationEventHandlers>());
        services.AddScoped<IDomainEventHandler<NotificationFailedEvent>>(sp => sp.GetRequiredService<NotificationEventHandlers>());

        // API token event handlers
        services.AddScoped<ApiTokenEventHandlers>();
        services.AddScoped<IDomainEventHandler<ApiTokenCreatedEvent>>(sp => sp.GetRequiredService<ApiTokenEventHandlers>());
        services.AddScoped<IDomainEventHandler<ApiTokenUsedEvent>>(sp => sp.GetRequiredService<ApiTokenEventHandlers>());
        services.AddScoped<IDomainEventHandler<ApiTokenRevokedEvent>>(sp => sp.GetRequiredService<ApiTokenEventHandlers>());

        // Email notification event handlers
        services.AddScoped<EmailNotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<UserRegisteredEvent>>(sp => sp.GetRequiredService<EmailNotificationEventHandlers>());
        //Temporary auto accept
        //services.AddScoped<IDomainEventHandler<WorkspaceMemberInvitedEvent>>(sp => sp.GetRequiredService<EmailNotificationEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateExpiringEvent>>(sp => sp.GetRequiredService<EmailNotificationEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateExpiredEvent>>(sp => sp.GetRequiredService<EmailNotificationEventHandlers>());

        // Webhook notification event handlers
        services.AddScoped<WebhookNotificationEventHandlers>();
        services.AddScoped<IDomainEventHandler<CertificateExpiringEvent>>(sp => sp.GetRequiredService<WebhookNotificationEventHandlers>());
        services.AddScoped<IDomainEventHandler<CertificateExpiredEvent>>(sp => sp.GetRequiredService<WebhookNotificationEventHandlers>());
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
        services.AddOptions<MessagingConfiguration>()
            .BindConfiguration(MessagingConfiguration.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEmailNotificationPublisher, RabbitMqEmailNotificationPublisher>();

        return services;
    }
}