using CertVal.Core.Messaging;
using CertVal.EmailService;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Services;

var builder = Host.CreateApplicationBuilder(args);

// Add service defaults for .NET Aspire
builder.AddServiceDefaults();

// Add RabbitMQ using Aspire
builder.AddRabbitMQClient("rabbitmq");

// Configure messaging settings from environment variables (set by Aspire)
builder.Services.AddOptions<MessagingConfiguration>()
    .BindConfiguration(MessagingConfiguration.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Configure EmailService settings
builder.Services.AddOptions<EmailServiceConfiguration>()
    .BindConfiguration(EmailServiceConfiguration.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Register services
builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

// Register the background service
builder.Services.AddHostedService<EmailWorkerService>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck("email-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var host = builder.Build();

try
{
    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Email Service failed to start");
    throw;
}