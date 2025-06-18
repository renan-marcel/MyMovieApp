using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MyMovieApp.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task Initialize(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var sp = scope.ServiceProvider;
        var environment = sp.GetRequiredService<IHostEnvironment>();

        if (environment.IsProduction()) return;

        var moviesDbContextFactory = sp.GetRequiredService<IDbContextFactory<MoviesDbContext>>();
        await using var moviesDbContext = await moviesDbContextFactory.CreateDbContextAsync();
        var connectionString = moviesDbContext.Database.GetConnectionString();
        if ((await moviesDbContext.Database.GetPendingMigrationsAsync()).Any())
            await moviesDbContext.Database.MigrateAsync();
    }
}