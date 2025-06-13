using Microsoft.EntityFrameworkCore;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.Data;

namespace MyMovieApp.Infrastructure.Repositories;

public class MovieRepository : IMovieRepository
{
    private readonly IDbContextFactory<MoviesDbContext> _contextFactory;

    public MovieRepository(IDbContextFactory<MoviesDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<Movie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Movies.AsSplitQuery()
            .Include(m => m.Reviews)
            .FirstOrDefaultAsync(m => m.ImdbId == imdbId, cancellationToken);
    }

    public async Task<List<Movie>> SearchMoviesAsync(string title, int? year, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(m => m.Title.Contains(title));

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        return await query.AsSplitQuery().Include(m => m.Reviews)
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateMovieAsync(Movie movie, CancellationToken cancellationToken)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existingMovie = await context.Movies.FirstOrDefaultAsync(m => m.ImdbId == movie.ImdbId, cancellationToken);

        if (existingMovie != null)
        {
            context.Entry(existingMovie).CurrentValues.SetValues(movie);
            context.Reviews.RemoveRange(existingMovie.Reviews);
            context.Reviews.AddRange(movie.Reviews);
        }
        else
        {
            await context.Movies.AddAsync(movie, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}