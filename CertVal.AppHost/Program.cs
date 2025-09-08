var builder = DistributedApplication.CreateBuilder(args);

var sqlportParametr = builder.AddParameter("database-port", secret: false);
var sqlportValue = await sqlportParametr.Resource.GetValueAsync(CancellationToken.None);
int sqlport = int.TryParse(sqlportValue, out var p) ? p : 1433;

var sqlpwd = builder.AddParameter("database-pwd", secret: true);

var db = builder.AddSqlServer("CertVal-sql-server", port: sqlport, password: sqlpwd)
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("CertVal-database");

var apiService = builder.AddProject<Projects.CertVal_ApiService>("CertVal-api-server")
    .WithReference(db)
    .WaitFor(db);

builder.Build().Run();
