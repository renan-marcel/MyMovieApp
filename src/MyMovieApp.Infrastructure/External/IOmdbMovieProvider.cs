using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Infrastructure.External
{
    public interface IOmdbMovieProvider
    {
        Task<Movie?> GetMovieByImdbIdAsync(string imdbId);
    }
}
