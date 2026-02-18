using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder
    .AddDockerComposeEnvironment("compose")
    .WithProperties(env =>
    {
        env.DashboardEnabled = false;
    });

var mongo = builder
    .AddMongoDB("mongo")
    .WithMongoExpress()
    .WithLifetime(ContainerLifetime.Persistent)
    .PublishAsDockerComposeService((_, _) => { });

var mongodb = mongo.AddDatabase("mongodb");

var api = builder
    .AddProject<CinemaList_Api>("CinemaList")
    .WithReference(mongodb)
    .WaitFor(mongodb)
    .WithEnvironment(
        "MongoDbSettings__ConnectionString",
        mongodb.Resource.ConnectionStringExpression
    )
    .PublishAsDockerComposeService((_, _) => { });
;

static string FindBun()
{
    var candidates = new[]
    {
        Environment.GetEnvironmentVariable("BUN_INSTALL"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".bun"),
    }
        .Where(p => p is not null)
        .Select(p => Path.Combine(p!, "bin", "bun"))
        .FirstOrDefault(File.Exists);

    return candidates;
}

var bunApp = builder
    .AddExecutable(
        name: "bun-api",
        command: FindBun(),
        workingDirectory: "../../frontend/src",
        args: ["run", "dev"]
    )
    .WithHttpEndpoint()
    .WaitFor(api);

builder.Build().Run();
