using CertVal.AppHost.Extensions;
using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var sqlpwd = builder.AddParameter("database-pwd", secret: true);
var minioUser = builder.AddParameter("minio-user", secret: true);
var minioUserPassword = builder.AddParameter("minio-user-password", secret: true);

var db = builder.AddPostgres("CertVal-sql-server", password: sqlpwd)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-sql-data", "/var/lib/postgresql/data")
    .AddDatabase("CertVal-database");

var rabbitmq = builder.AddRabbitMQ("CertVal-rabbitmq")
    .WithManagementPlugin()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithVolume("certval-rabbitmq-data", "/var/lib/rabbitmq");

var minio = builder.AddMinioContainer("CertVal-minio", rootUser: minioUser, rootPassword: minioUserPassword)
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
    .WithReference(minio)
    .WithMessagingConfig(builder.Configuration)
    .WaitFor(db)
    .WaitFor(rabbitmq)
    .WaitFor(minio)
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