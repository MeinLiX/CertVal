using CertVal.Infrastructure.Data;
using CertVal.Infrastructure.Services;
using CertVal.Infrastructure;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("api", o =>
{

});

builder.Services.AddHttpContextAccessor();

//TOdo bug with worker and reg IDomainEventDispatcher
builder.AddSqlServerDbContext<ApplicationDbContext>(
    connectionName: "CertVal-database",
    configureDbContextOptions: options =>
    {
        DatabaseConfiguration.ConfigureApplicationDbContext(
            options,
            builder.Configuration
        );
    }
);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<CertificateExpiryCheckerService>();

var app = builder.Build();

app.UseExceptionHandler();

//TODO: MOVE TO OUT
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(o =>
    {
        o.WithTitle("CertVal API");
        o.WithTheme(ScalarTheme.Default);
        o.WithDarkMode(false);
        o.WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
        o.Servers = Array.Empty<ScalarServer>(); //url fix for container host
    });

    app.MapGet("/", [ExcludeFromDescription] () => Results.Redirect("/scalar/api"));

    var groupDB = app.MapGroup("db").WithTags("db");
    groupDB.MapGet("/create", (ApplicationDbContext db) =>
    {
        if (!db.EnsureCreate()) return Results.BadRequest();
        db.SaveChanges();
        return Results.Ok();
    })
    .WithDescription("ONLY FOR DEBUGGER")
    .WithName("DataBaseCreate")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

    groupDB.MapGet("/delete", (ApplicationDbContext db) =>
    {
        if (!db.EnsureDelete()) return Results.BadRequest();
        db.SaveChanges();
        return Results.Ok();
    })
    .WithDescription("ONLY FOR DEBUGGER")
    .WithName("DataBaseDelete")
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest);

    groupDB.MapPost("/check-expiry", async (IServiceProvider serviceProvider) =>
    {
        using var scope = serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<CertVal.Core.Repositories.IUnitOfWork>();

        var certificates = await unitOfWork.Certificates.GetExpiringAsync(90);
        var eventCount = 0;

        foreach (var cert in certificates)
        {
            cert.CheckExpiry();
            if (cert.DomainEvents.Any())
                eventCount++;
        }

        await unitOfWork.SaveChangesAsync();

        return Results.Ok(new
        {
            CertificatesChecked = certificates.Count(),
            EventsTriggered = eventCount
        });
    })
    .WithDescription("ONLY FOR DEBUGGER - Manually trigger certificate expiry checks")
    .WithName("TriggerExpiryCheck");

    var groupEvents = app.MapGroup("api/events").WithTags("events");

    groupEvents.MapGet("/", async (IServiceProvider serviceProvider, int take = 50) =>
    {
        using var scope = serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<CertVal.Core.Repositories.IEventStoreRepository>();

        var events = await eventStore.GetRecentEventsAsync(take);

        return Results.Ok(events.Select(e => new
        {
            e.Id,
            e.EventId,
            e.EventType,
            e.AggregateType,
            e.AggregateId,
            e.UserId,
            e.CorrelationId,
            e.OccurredAt,
            e.StoredAt,
            EventData = System.Text.Json.JsonSerializer.Deserialize<object>(e.EventData),
            Metadata = e.GetMetadata()
        }));
    })
   .WithDescription("Get events by type")
   .WithName("GetEventsByType");

    groupEvents.MapGet("/stats", async (IServiceProvider serviceProvider) =>
    {
        using var scope = serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<CertVal.Core.Repositories.IEventStoreRepository>();

        var totalEvents = await eventStore.GetEventCountAsync();
        var recentEvents = await eventStore.GetRecentEventsAsync(100);

        var eventTypeStats = recentEvents
            .GroupBy(e => e.EventType)
            .Select(g => new { EventType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var aggregateTypeStats = recentEvents
            .Where(e => !string.IsNullOrEmpty(e.AggregateType))
            .GroupBy(e => e.AggregateType)
            .Select(g => new { AggregateType = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var hourlyStats = recentEvents
            .GroupBy(e => e.OccurredAt.Hour)
            .Select(g => new { Hour = g.Key, Count = g.Count() })
            .OrderBy(x => x.Hour)
            .ToList();

        return Results.Ok(new
        {
            TotalEvents = totalEvents,
            RecentEventTypes = eventTypeStats,
            RecentAggregateTypes = aggregateTypeStats,
            HourlyDistribution = hourlyStats,
            OldestEvent = recentEvents.LastOrDefault()?.OccurredAt,
            NewestEvent = recentEvents.FirstOrDefault()?.OccurredAt
        });
    })
    .WithDescription("Get event store statistics")
    .WithName("GetEventStats");

    groupEvents.MapGet("/stream", async (
        IServiceProvider serviceProvider,
        long? afterEventId = null,
        int take = 100) =>
    {
        using var scope = serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<CertVal.Core.Repositories.IEventStoreRepository>();

        var events = afterEventId.HasValue
            ? await eventStore.GetEventsAfterAsync(afterEventId.Value, take)
            : await eventStore.GetRecentEventsAsync(take);

        return Results.Ok(new
        {
            Events = events.Select(e => new
            {
                e.Id,
                e.EventId,
                e.EventType,
                e.AggregateType,
                e.AggregateId,
                e.UserId,
                e.CorrelationId,
                e.OccurredAt,
                e.StoredAt,
                EventData = System.Text.Json.JsonSerializer.Deserialize<object>(e.EventData),
                Metadata = e.GetMetadata()
            }),
            LastEventId = events.LastOrDefault()?.Id,
            HasMore = events.Count() == take
        });
    })
    .WithDescription("Stream events for real-time monitoring")
    .WithName("StreamEvents");

    groupEvents.MapGet("/{eventId:long}", async (
        IServiceProvider serviceProvider,
        long eventId) =>
    {
        using var scope = serviceProvider.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<CertVal.Core.Repositories.IEventStoreRepository>();

        var storedEvent = await eventStore.GetEventByIdAsync(eventId);

        if (storedEvent == null)
            return Results.NotFound();

        return Results.Ok(new
        {
            storedEvent.Id,
            storedEvent.EventId,
            storedEvent.EventType,
            storedEvent.AggregateType,
            storedEvent.AggregateId,
            storedEvent.UserId,
            storedEvent.CorrelationId,
            storedEvent.OccurredAt,
            storedEvent.StoredAt,
            EventData = System.Text.Json.JsonSerializer.Deserialize<object>(storedEvent.EventData),
            Metadata = storedEvent.GetMetadata(),
            RawEventData = storedEvent.EventData,
            RawMetadata = storedEvent.Metadata
        });
    })
    .WithDescription("Get single event details")
    .WithName("GetEventById");
}

app.MapDefaultEndpoints();

try
{
    await app.Services.InitializeDatabaseAsync();
    await app.RunAsync();
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "Application failed to start");
    throw;
}