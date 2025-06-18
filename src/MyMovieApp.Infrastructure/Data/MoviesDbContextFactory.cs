using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MyMovieApp.Infrastructure.Data;

public class MoviesDbContextFactory : IDesignTimeDbContextFactory<MoviesDbContext>
{
    public MoviesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MoviesDbContext>();
        optionsBuilder.UseNpgsql();
        return new MoviesDbContext(optionsBuilder.Options);
    }
}