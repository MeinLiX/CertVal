using Aspire.Hosting;
using CertVal.AppHost.Extensions;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var sqlportParametr = builder.AddParameter("database-port", secret: false);
var sqlportValue = await sqlportParametr.Resource.GetValueAsync(CancellationToken.None);
int sqlport = int.TryParse(sqlportValue, out var p) ? p : 1433;

var sqlpwd = builder.AddParameter("database-pwd", secret: true);

var db = builder.AddSqlServer("CertVal-sql-server", port: sqlport, password: sqlpwd)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("CertVal-database");

var rabbitmq = builder.AddRabbitMQ("rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent);

var emailService = builder.AddProject<Projects.CertVal_EmailService>("email-service")
    .WithReference(rabbitmq)
    .WithMessagingConfig(builder.Configuration)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(rabbitmq);

var apiService = builder.AddProject<Projects.CertVal_ApiService>("CertVal-api-server")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithMessagingConfig(builder.Configuration)
    .WaitFor(db)
    .WaitFor(rabbitmq)
    .WaitFor(emailService)
    .PublishAsDockerFile();

var webPort = builder.Configuration.GetValue<int>("Web:Port");
var web = builder.AddNpmApp("web", "../CertVal.Web", scriptName: "dev", args: ["--port", webPort.ToString()])
    .WithNpmPackageInstallation()
    .WithReference(apiService).WaitFor(apiService)
    .WithEnvironment("VITE_API_BASE_URL", builder.Configuration.GetValue<string>("Web:ApiUrl") ?? throw new InvalidOperationException("Missing configuration: Web:ApiUrl"))
    .WithHttpEndpoint(port: webPort, isProxied: false)
    .PublishAsDockerFile();

await builder.Build().RunAsync();