var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.hero_Web>("hero-web");

builder.Build().Run();
