using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.Data;
using MyMovieApp.Infrastructure.External;
using MyMovieApp.Infrastructure.Repositories;
using Polly;
using Polly.CircuitBreaker;
using Refit;

namespace MyMovieApp.Infrastructure.Hosting;

/// <summary>
///     Provides extension methods for registering infrastructure services in the ASP.NET Core dependency injection
///     container.
/// </summary>
public static class HostingExtensions
{
    /// <summary>
    ///     Registers the infrastructure services required by the application, including
    ///     external API clients, data access layer, and resilience policies.
    /// </summary>
    /// <param name="services">The service collection to which infrastructure services will be added.</param>
    /// <param name="configuration">The application configuration instance.</param>
    /// <returns>The updated <see cref="IServiceCollection" /> instance.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ValidateApiUrl(configuration);

        services.AddCircuitBreakerResiliencePipeline()
            .AddRefitAndResiliantHandler(configuration)
            .AddDataLayer(configuration)
            .AddApplicationHealtyChecks(configuration);

        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    /// <summary>
    ///     Configures and registers the Refit client for the OMDb API with custom JSON serialization options
    ///     and attaches a resilience handler for retry and timeout strategies.
    /// </summary>
    /// <param name="services">The service collection to which the Refit client will be added.</param>
    /// <param name="configuration">The application configuration instance.</param>
    private static IServiceCollection AddRefitAndResiliantHandler(this IServiceCollection services, IConfiguration configuration)
    {
        var jsonSerializerOptions = JsonSerializerOptionsCreator();

        services.AddRefitClient<IOmdbApi>(new RefitSettings
        {
            // Use o SystemTextJsonContentSerializer com as opções configuradas
            ContentSerializer = new SystemTextJsonContentSerializer(jsonSerializerOptions)
        })
            .ConfigureHttpClient(c =>
            {
                c.BaseAddress = new Uri(configuration["Omdb:ApiUri"] ?? throw new InvalidOperationException("The configuration value for 'Omdb:ApiUri' must not be null or empty."));
            })
            .AddResilienceHandler("my-circuit-breaker", builder =>
            {
                // Refer to https://www.pollydocs.org/strategies/retry.html#defaults for retry defaults
                builder.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = 4,
                    Delay = TimeSpan.FromSeconds(2),
                    BackoffType = DelayBackoffType.Exponential
                });

                // Refer to https://www.pollydocs.org/strategies/timeout.html#defaults for timeout defaults
                builder.AddTimeout(TimeSpan.FromSeconds(5));
            });
        return services;
    }

    /// <summary>
    ///     Registers the application's data access layer, including the database context factory
    ///     and repository implementations and the interceptor, with the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection to which data layer services will be added.</param>
    /// <param name="configuration">The application configuration instance.</param>
    private static IServiceCollection AddDataLayer(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IOmdbMovieProvider, OmdbMovieProvider>();
        services.AddSingleton<QueryInterceptor>();

        services.AddPooledDbContextFactory<MoviesDbContext>((sp, options) =>
        {
            options.UseNpgsql(configuration["ConnectionString:Database"] ?? "", sql =>
            {
                sql.MigrationsHistoryTable("__EFMigrationsHistory");
                sql.MigrationsAssembly(typeof(MoviesDbContext).GetTypeInfo().Assembly.GetName().Name);
            });
            options.AddInterceptors(sp.GetRequiredService<QueryInterceptor>());
        });

        services.AddScoped<IMovieRepository, MovieRepository>();

        return services;
    }

    /// <summary>
    ///     Creates and configures <see cref="JsonSerializerOptions" /> for use with System.Text.Json.
    ///     The options are set to be case-insensitive for property names and use the default type info resolver,
    ///     enabling reflection-based serialization and deserialization.
    /// </summary>
    /// <returns>
    ///     A configured <see cref="JsonSerializerOptions" /> instance suitable for use with Refit and System.Text.Json.
    /// </returns>
    private static JsonSerializerOptions JsonSerializerOptionsCreator()
    {
        // Crie as opções do serializador para usar reflection
        var jsonSerializerOptions = new JsonSerializerOptions
        {
            // É uma boa prática ignorar o case das propriedades
            PropertyNameCaseInsensitive = true,
            // Esta é a linha chave que resolve o problema!
            TypeInfoResolver = new DefaultJsonTypeInfoResolver()
        };
        return jsonSerializerOptions;
    }

    /// <summary>
    ///     Validates that the OMDb API URI is present and not empty in the application configuration.
    ///     This method checks the configuration for the "Omdb:ApiUri" key, which is required for the application
    ///     to communicate with the OMDb external API. If the value is missing or empty, an InvalidOperationException is thrown
    ///     to prevent the application from starting with invalid or incomplete configuration.
    /// </summary>
    /// <param name="configuration">The application configuration instance to retrieve the OMDb API URI from.</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the "Omdb:ApiUri" configuration value is null, empty, or consists only of white-space characters.
    /// </exception>
    private static void ValidateApiUrl(IConfiguration configuration)
    {
        var apiKey = configuration["Omdb:ApiUri"];
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new InvalidOperationException("The configuration value for 'Omdb:ApiKey' must not be null or empty.");
    }

    /// <summary>
    ///     Adds a named resilience pipeline to the service collection using a circuit breaker strategy.
    ///     This pipeline monitors HTTP requests and temporarily blocks further requests when a specified failure threshold is
    ///     reached,
    ///     helping to prevent cascading failures and improve system stability.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection" /> to which the resilience pipeline will be added.</param>
    /// </summary>
    private static IServiceCollection AddCircuitBreakerResiliencePipeline(this IServiceCollection services)
    {
        // Adiciona um pipeline de resiliência nomeado com estratégia de circuit breaker
        services.AddResiliencePipeline("my-circuit-breaker", pipelineBuilder =>
        {
            pipelineBuilder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
            {
                FailureRatio = 0.5, // Quebra se 50% das requisições falharem
                SamplingDuration = TimeSpan.FromSeconds(60), // Janela de 60 segundos
                MinimumThroughput = 7, // Pelo menos 7 requisições na janela
                BreakDuration = TimeSpan.FromSeconds(30), // Permanece quebrado por 30 segundos
                OnOpened = args =>
                {
                    Console.WriteLine("Circuit breaker opened!");
                    return default;
                },
                OnClosed = args =>
                {
                    Console.WriteLine("Circuit breaker closed!");
                    return default;
                },
                OnHalfOpened = args =>
                {
                    Console.WriteLine("Circuit breaker is now half-open, next request is a trial.");
                    return default;
                }
            });
        });
        return services;
    }

    private static IServiceCollection AddApplicationHealtyChecks(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHealthChecks()
            .AddNpgSql(connectionString: configuration["ConnectionString:Database"],
                name: "Instancia do Postgres");

        return services;
    }
}