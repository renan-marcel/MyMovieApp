using MyMovieApp.Infrastructure.External.Models;
using Refit;

namespace MyMovieApp.Infrastructure.External;

public interface IOmdbApi
{
    // Exemplo de método para buscar detalhes de um filme pelo IMDb ID
    [Get("/?apikey={apiKey}&i={imdbId}")]
    Task<OmdbResponse?> GetMovieAsync(string apiKey, string imdbId);
}
