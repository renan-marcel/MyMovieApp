using MyMovieApp.Infrastructure.External.Models;
using Refit;

namespace MyMovieApp.Infrastructure.External;

public interface IOmdbApi
{
    [Get("/?apikey={apiKey}&t={title}&y={year}")]
    Task<OmdbResponse?> GetMovieByTitleAsync(string apiKey, string title, short year);

    [Get("/?apikey={apiKey}&i={imdbId}")]
    Task<OmdbResponse?> GetMovieByImdbIdAsync(string apiKey, string imdbId);

    [Get("/?apikey={apiKey}&t={title}")]
    Task<OmdbResponse?> GetMovieByTitleAsync(string apiKey, string title);
}