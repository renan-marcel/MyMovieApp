using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Interfaces;

public interface IMovieRepository
{
    Task<Movie?> GetByImdbIdAsync(string imdbId, CancellationToken cancellationToken);
    Task AddOrUpdateMovieAsync(Movie movie, CancellationToken cancellationToken);
    Task<List<Movie>> SearchMoviesAsync(string title, int? year, CancellationToken cancellationToken);
}