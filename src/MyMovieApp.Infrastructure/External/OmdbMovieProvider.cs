using Microsoft.Extensions.Configuration;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Infrastructure.External;

public class OmdbMovieProvider : IOmdbMovieProvider
{
    private readonly IOmdbApi _omdbApi;
    private readonly string _apiKey;

    public OmdbMovieProvider(IOmdbApi omdbApi, IConfiguration configuration)
    {
        _omdbApi = omdbApi;
        _apiKey = configuration["Omdb:ApiKey"];
    }

    public async Task<Movie?> GetMovieByImdbIdAsync(string imdbId)
    {
        var response = await _omdbApi.GetMovieAsync(_apiKey, imdbId);
        if (response is null)
            return null;

        var movie = Movie.Create(response.ImdbID, response.Title, response.Year);

        movie.Genre = response.Genre ?? "N/A";
        movie.Director = response.Director ?? "N/A";
        movie.ImdbRating = response.ImdbRating ?? "N/A";
        movie.Plot = response.Plot ?? "N/A";

        var actors = response.Actors?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList() ?? [];

        actors.ForEach(i => movie.AddNewActor(i));

        return movie;
    }
}
