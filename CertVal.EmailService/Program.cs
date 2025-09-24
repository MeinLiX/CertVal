using CertVal.Core.Messaging;
using CertVal.EmailService;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Services;
using CertVal.EmailService.Templates;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("CertVal-rabbitmq");

builder.Services.AddOptions<MessagingConfiguration>()
    .BindConfiguration(MessagingConfiguration.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<EmailServiceConfiguration>()
    .BindConfiguration(EmailServiceConfiguration.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<ITemplateRenderer, TemplateRenderer>();
builder.Services.AddSingleton<ITemplateService, TemplateService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

builder.Services.AddHostedService<EmailWorkerService>();

builder.Services.AddHealthChecks()
    .AddCheck("email-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

builder.Logging.SetMinimumLevel(LogLevel.Information);

var host = builder.Build();

try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Email Service");

    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Email Service failed to start");
    throw;
}