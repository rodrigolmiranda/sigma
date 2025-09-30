using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add PostgreSQL database
var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var sigmaDb = postgres.AddDatabase("sigma");

// Add the API project
var api = builder.AddProject<Projects.Sigma_API>("api")
    .WithReference(sigmaDb)
    .WithExternalHttpEndpoints();

// Add Workers project (for background processing)
var workers = builder.AddProject<Projects.Sigma_Workers>("workers")
    .WithReference(sigmaDb);

builder.Build().Run();