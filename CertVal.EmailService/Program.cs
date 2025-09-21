using CertVal.Core.Messaging;
using CertVal.EmailService;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Services;
using Microsoft.Extensions.Options;

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

builder.Services.AddSingleton<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddSingleton<IEmailService, SmtpEmailService>();
builder.Services.AddSingleton<IRabbitMqService, RabbitMqService>();

builder.Services.AddHostedService<EmailWorkerService>();

builder.Services.AddHealthChecks()
    .AddCheck("email-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

var host = builder.Build();

try
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Starting Email Service...");

    var emailConfig = host.Services.GetRequiredService<IOptions<EmailServiceConfiguration>>().Value;
    logger.LogInformation("SMTP Host: {Host}, Port: {Port}", emailConfig.Smtp.Host, emailConfig.Smtp.Port);

    await host.RunAsync();
}
catch (Exception ex)
{
    var logger = host.Services.GetService<ILogger<Program>>();
    logger?.LogCritical(ex, "Email Service failed to start");
    throw;
}