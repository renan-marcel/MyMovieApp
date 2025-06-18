using MyMovieApp.Infrastructure.External.Models;

namespace MyMovieApp.Infrastructure.External;

public static class OmdbApiExtensions
{
    public static async Task<OmdbResponse?> GetMovieByTitle(this IOmdbApi omdbApi, string apiKey, string title,
        short? year = null)
    {
        if (year.HasValue) return await omdbApi.GetMovieByTitleAsync(apiKey, title, year.Value);
        return await omdbApi.GetMovieByTitleAsync(apiKey, title);
    }
}