using MyMovieApp.API;
using MyMovieApp.Application.Hosting;
using MyMovieApp.Application.Interfaces;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.Data;
using MyMovieApp.Infrastructure.Hosting;
using Serilog;

var builder = WebApplication.CreateSlimBuilder(args);

// Configurar o Serilog para usar o Seq
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();

    configuration.WriteTo.Seq(builder.Configuration["Seq:ServerUrl"]);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();

// health checks to monitor the application's health status
builder.Services.AddHealthChecks();

var app = builder.Build();

await DbInitializer.Initialize(app.Services);

var omdbApi = app.MapGroup("Omdb");

omdbApi.MapGet("/movie/{imdbId}", async (CancellationToken token, string imdbId, IMovieService movieService) =>
{
    var movie = await movieService.GetMovieByImdbIdAsync(imdbId, token);
    return movie is not null ? Results.Ok(movie) : Results.NotFound();
});

app.MapHealthChecks("/health");

app.Run();