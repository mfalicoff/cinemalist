using Projects;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<CinemaList_Api>("CinemaList");

builder.Build().Run();
