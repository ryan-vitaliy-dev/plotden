using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using backend.Infrastructure.Persistence;
using backend.Features.Accounts;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc.Abstractions;
using backend.Infrastructure.Email;


WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("AppDb"))
        .LogTo(Console.WriteLine, LogLevel.Information));

builder.Services.AddLocalization();

string[] supportedCultures = ["en"];
builder.Services.AddRequestLocalization(options =>

    options.SetDefaultCulture("en")
    .AddSupportedCultures(supportedCultures)
);

// To be added when I do frontend
// builder.Services.AddCors(options =>
//     {
//         options.AddPolicy("AllowFrontend",
//             builder =>
//             {
//                 builder.WithOrigins("FRONTEND_DOMAIN_HERE")
//                        .AllowAnyMethod()
//                        .AllowAnyHeader()
//                        .AllowCredentials();
//             });
//     });

builder.Services.Configure<PasswordHasherOptions>(options =>
{
    //0x01 | format marker version | PRF (pseudo-random function) | iteration count | salt length | salt | subkey
    options.IterationCount = 200000;
});

builder.Services.AddTransient<IEmailSender, FluentEmailSender>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddControllers()
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (type, factory) =>
        {
            var assemblyName = new AssemblyName(typeof(backend.Resources.SharedResource).GetType().Assembly.FullName!);
            return factory.Create(nameof(backend.Resources.SharedResource), assemblyName.Name!);
        };
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            string errorMessage;
            var errors = context.ModelState
                .SelectMany(kvp => kvp.Value!.Errors)
                .Where(e => e.Exception == null)
                .Select(e => e.ErrorMessage)
            .ToList();

            var firstError = context.ModelState.First(); // Always guaranteed to have one or more errors
            string fieldName = firstError.Key;
            if(fieldName.StartsWith('$') || fieldName.Contains("DTO"))
            {
                errorMessage = "There were errors present in the request and it could not be processed.";
            }
            else
            {
                errorMessage = firstError.Value!.Errors.First().ErrorMessage;
            }

            return new BadRequestObjectResult(new { message = errorMessage });
        };
    });

WebApplication webApplicationServer = builder.Build();

// Configure the HTTP request pipeline.
// if (webApplicationServer.Environment.IsDevelopment())
// {
//     webApplicationServer.MapOpenApi();
// }

if(!webApplicationServer.Environment.IsDevelopment())
{
    webApplicationServer.UseHttpsRedirection();
    webApplicationServer.UseHsts();
}

webApplicationServer.UseRequestLocalization();

//webApplicationServer.UseCors("AllowFrontend");
//webApplicationServer.UseAuthentication(); - must be after UseRequestLocalization
//webApplicationServer.UseAuthorization(); - must be after UseRequestLocalization
webApplicationServer.MapControllers(); // - must be after UseRequestLocalization

webApplicationServer.Run();

// var summaries = new[]
// {
//     "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
// };

// webapp.MapGet("/weatherforecast", () =>
// {
//     var forecast =  Enumerable.Range(1, 5).Select(index =>
//         new WeatherForecast
//         (
//             DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
//             Random.Shared.Next(-20, 55),
//             summaries[Random.Shared.Next(summaries.Length)]
//         ))
//         .ToArray();
//     return forecast;
// })
// .WithName("GetWeatherForecast");

// webapp.Run();

// record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
// {
//     public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
// }
