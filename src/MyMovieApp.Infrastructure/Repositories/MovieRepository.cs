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

    public async Task<Movie?> GetByImdbIdAsync(CancellationToken cancellationToken, string imdbId)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Movies.AsSplitQuery()
            .AsTracking()
            .Include(m => m.Reviews)
            .Include(m => m.Actor)
            .FirstOrDefaultAsync(m => m.ImdbId == imdbId, cancellationToken);
    }

    public async Task<Movie?> GetByTitleAsync(CancellationToken cancellationToken, string title, short? year)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var query = context.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(m => m.Title.Contains(title));

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);


        return await query.AsSplitQuery()
            .AsTracking()
            .Include(m => m.Reviews)
            .Include(m => m.Actor)
            .FirstOrDefaultAsync(m => m.Title == title, cancellationToken);
    }

    public async Task<List<Movie>> SearchMoviesAsync(CancellationToken cancellationToken, string title, short? year)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = context.Movies.AsQueryable();

        if (!string.IsNullOrEmpty(title))
            query = query.Where(m => m.Title.Contains(title));

        if (year.HasValue)
            query = query.Where(m => m.Year == year.Value);

        return await query.AsSplitQuery()
            .AsTracking()
            .Include(m => m.Reviews)
            .Include(m => m.Actor)
            .ToListAsync(cancellationToken);
    }

    public async Task AddOrUpdateMovieAsync(CancellationToken cancellationToken, Movie movie)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var existingMovie = await context.Movies.FirstOrDefaultAsync(m => m.ImdbId == movie.ImdbId, cancellationToken);

        if (existingMovie != null)
        {
            context.Entry(existingMovie).CurrentValues.SetValues(movie);

            var itensReviews = movie.Reviews
                .Where(e => e.Movie is null)
                .ToList();

            context.Reviews.AddRange(itensReviews);
        }
        else
        {
            await context.Movies.AddAsync(movie, cancellationToken);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}