using CertVal.AppHost.Extensions;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var sqlpwd = builder.AddParameter("database-pwd", secret: true);
var minioUser = builder.AddParameter("minio-user", secret: true);
var minioUserPassword = builder.AddParameter("minio-user-password", secret: true);

var db = builder.AddPostgres("CertVal-sql-server", password: sqlpwd)
    .WithImageTag("18") //sync version with docker compose
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-sql-data", "/var/lib/postgresql/data")
    .AddDatabase("CertVal-database");

var rabbitmq = builder.AddRabbitMQ("CertVal-rabbitmq")
    .WithImageTag("4.1.4-management") //sync version with docker compose
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-rabbitmq-data", "/var/lib/rabbitmq");

var redis = builder.AddRedis("CertVal-redis")
    .WithImageTag("8.4") //sync version with docker compose
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-redis-data", "/data");

var minio = builder.AddMinioContainer("CertVal-minio", rootUser: minioUser, rootPassword: minioUserPassword)
    .WithImageTag("RELEASE.2025-09-07T16-13-09Z") //sync version with docker compose
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-minio-data", "/data");

var emailService = builder.AddProject<Projects.CertVal_EmailService>("email-service")
    .WithReference(rabbitmq)
    .WithMessagingConfig(builder.Configuration)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
    .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
    .WaitFor(rabbitmq);

var apiService = builder.AddProject<Projects.CertVal_ApiService>("CertVal-api-server")
    .WithReference(db)
    .WithReference(rabbitmq)
    .WithReference(redis)
    .WithReference(minio)
    .WithMessagingConfig(builder.Configuration)
    .WaitFor(db)
    .WaitFor(rabbitmq)
    .WaitFor(minio)
    .PublishAsDockerFile();

var webPort = builder.Configuration.GetValue<int>("Web:Port");
var web = builder.AddNpmApp("web", "../CertVal.Web", scriptName: "dev", args: ["--port", webPort.ToString()])
    .WithNpmPackageInstallation()
    .WithReference(apiService)
    .WithEnvironment("VITE_API_BASE_URL", builder.Configuration.GetValue<string>("Web:ApiUrl") ?? throw new InvalidOperationException("Missing configuration: Web:ApiUrl"))
    .WithEnvironment("VITE_GOOGLE_CLIENT_ID", builder.Configuration.GetValue<string>("Web:VITE_GOOGLE_CLIENT_ID") ?? throw new InvalidOperationException("Missing configuration: Web:VITE_GOOGLE_CLIENT_ID"))
    .WithHttpEndpoint(port: webPort, isProxied: false)
    .WaitFor(apiService)
    .PublishAsDockerFile();

await builder.Build().RunAsync();