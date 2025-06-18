using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Interfaces;

public interface IMovieRepository
{
    Task<Movie?> GetByImdbIdAsync(CancellationToken cancellationToken, string imdbId);
    Task<Movie?> GetByTitleAsync(CancellationToken cancellationToken, string title, short? year);
    Task AddOrUpdateMovieAsync(CancellationToken cancellationToken, Movie movie);
    Task<List<Movie>> SearchMoviesAsync(CancellationToken cancellationToken, string title, short? year);
}