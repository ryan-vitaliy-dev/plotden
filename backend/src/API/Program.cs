using API;
using Application;
using Infrastructure;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddInfrastructure(builder.Configuration)
    .AddApplication()
    .AddApiServices(builder.Configuration);

WebApplication webApplication = builder.Build();
webApplication.UseApiMiddleware();
webApplication.Run();