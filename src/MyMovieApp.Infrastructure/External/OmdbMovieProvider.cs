
using Microsoft.Extensions.Configuration;
using MyMovieApp.Domain;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Infrastructure.External.Models;

namespace MyMovieApp.Infrastructure.External;

public class OmdbMovieProvider : IOmdbMovieProvider
{
    private readonly IConfiguration _configuration;
    private readonly IOmdbApi _omdbApi;

    public OmdbMovieProvider(IOmdbApi omdbApi, IConfiguration configuration)
    {
        _omdbApi = omdbApi ?? throw new ArgumentNullException(nameof(omdbApi));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<Movie?> GetMovieByImdbIdAsync(string imdbId)
    {
        if (string.IsNullOrWhiteSpace(imdbId))
            throw new ArgumentException("IMDb ID cannot be null or empty.", nameof(imdbId));

        var apiKey = _configuration["OMDb:ApiKey"] ??
                     throw new InvalidOperationException("API key is not configured.");
        var response = await _omdbApi.GetMovieByImdbIdAsync(apiKey, imdbId);
        if (response is null)
            return null;

        var movie = MapMovie(response);

        return movie;
    }

    public async Task<Movie?> GetMovieByTitleAsync(string title, short? year)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));

        if (year.HasValue && year < Constants.FirstYearMovie)
            throw new ArgumentOutOfRangeException(nameof(year), $"Year cannot be less than {Constants.FirstYearMovie}.");

        var apiKey = _configuration["OMDb:ApiKey"] ??
                     throw new InvalidOperationException("API key is not configured.");

        var response = await _omdbApi.GetMovieByTitle(apiKey, title, year);
        if (response is null)
            return null;

        var movie = MapMovie(response);

        return movie;
    }

    private static Movie MapMovie(OmdbResponse response)
    {
        if (!short.TryParse(response.Year, out var year))
            throw new InvalidOperationException($"invalid year returned from OMDb API: '{response.Year}'.");

        var movie = Movie.Create(response.ImdbID, response.Title, year);
        
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