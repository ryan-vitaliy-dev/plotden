using Infrastructure;
using Application;
using API;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApiServices(builder.Configuration);

WebApplication webApplication = builder.Build();
webApplication.UseApiMiddleware();
webApplication.Run();