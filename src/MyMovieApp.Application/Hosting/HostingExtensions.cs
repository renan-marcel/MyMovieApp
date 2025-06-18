using Microsoft.Extensions.DependencyInjection;
using MyMovieApp.Application.Interfaces;
using MyMovieApp.Application.Services;

namespace MyMovieApp.Application.Hosting;

public static class HostingExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register application services here
        services.AddScoped<IMovieService, MovieService>();

        // Add other application services as needed
        return services;
    }
}