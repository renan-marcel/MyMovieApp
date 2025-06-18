using Microsoft.AspNetCore.Mvc;
using MyMovieApp.API;
using MyMovieApp.Application.DTOs;
using MyMovieApp.Application.Hosting;
using MyMovieApp.Application.Interfaces;
using MyMovieApp.Infrastructure.Data;
using MyMovieApp.Infrastructure.Hosting;
using Scalar.AspNetCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Services(services)
        .Enrich.FromLogContext();
    var seqUrl = builder.Configuration["Seq:ServerUrl"];
    if (!string.IsNullOrEmpty(seqUrl)) configuration.WriteTo.Seq(seqUrl);
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, AppJsonSerializerContext.Default);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapScalarApiReference(c =>
    {
        c.DotNetFlag = true;
        c.OpenApiRoutePattern = "/swagger/v1/swagger.json";
    });
}

await DbInitializer.Initialize(app.Services);

app.MapGet("/search-movie", async (CancellationToken token, [FromQuery(Name = "title")] string title,
        [FromQuery(Name = "year")] short? year, IMovieService movieService) =>
    {
        var searchRequest = new SearchRequestDto(title, year);

        var movies = await movieService.GetMovieByTitleAsync(token, searchRequest);
        return Results.Ok(movies);
    })
    .WithName("PostSearchMovie")
    .WithOpenApi();

app.MapPost("/create-movie",
        async (CancellationToken token, CreateMovieReviewDto requestBody, IMovieService movieService) =>
        {
            var movies = await movieService.CreateMovieReviewAsync(token, requestBody);
            return Results.Ok(movies);
        })
    .WithName("PostCreateMovie")
    .WithOpenApi();


app.MapHealthChecks("/health");

app.Run();