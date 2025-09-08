using CertVal.Infrastructure.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi("api", o =>
{
    
});

builder.AddSqlServerDbContext<ApplicationDbContext>(connectionName: "CertVal-database");

var app = builder.Build();

app.UseExceptionHandler();

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
    //.ExcludeFromDescription();

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
}


app.MapDefaultEndpoints();

app.Run();
