using CertVal.Core.Messaging;
using CertVal.EmailService;
using CertVal.EmailService.Configuration;
using CertVal.EmailService.Services;
using CertVal.EmailService.Services.Abstractions;
using CertVal.EmailService.Templates;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddRabbitMQClient("CertVal-rabbitmq");

builder.Services.Configure<HostOptions>(o => o.ShutdownTimeout = TimeSpan.FromSeconds(30));

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
    .AddCheck<CertVal.EmailService.HealthChecks.RabbitMqHealthCheck>("rabbitmq")
    .AddCheck<CertVal.EmailService.HealthChecks.SmtpHealthCheck>("smtp")
    .AddCheck("email-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());

var host = builder.Build();

await host.RunAsync();
