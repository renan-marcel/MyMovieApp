using MyMovieApp.Application.DTOs;
using MyMovieApp.Application.Interfaces;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.External;

namespace MyMovieApp.Application.Services;

public class MovieService : IMovieService
{
    private readonly IMovieRepository _movieRepository;
    private readonly IOmdbMovieProvider _omdbProvider;

    public MovieService(IMovieRepository movieRepository, IOmdbMovieProvider omdbProvider)
    {
        _movieRepository = movieRepository;
        _omdbProvider = omdbProvider;
    }

    public async Task<Movie> GetMovieByImdbIdAsync(CancellationToken cancellationToken, string imdbId)
    {
        var movie = await _movieRepository.GetByImdbIdAsync(cancellationToken, imdbId);

        if (movie == null)
        {
            // Fetch from OMDb if not in local database
            movie = await _omdbProvider.GetMovieByImdbIdAsync(imdbId);
            if (movie != null)
                await _movieRepository.AddOrUpdateMovieAsync(cancellationToken, movie);
            else
                throw new KeyNotFoundException($"Movie with IMDb ID {imdbId} not found.");
        }

        return movie;
    }

    public async Task<Movie> GetMovieByTitleAsync(CancellationToken cancellationToken, SearchRequestDto searchRequest)
    {
        var movie = await _movieRepository.GetByTitleAsync(cancellationToken, searchRequest.Title, searchRequest.Year);

        if (movie == null)
        {
            // Fetch from OMDb if not in local database
            movie = await _omdbProvider.GetMovieByTitleAsync(searchRequest.Title, searchRequest.Year);
            if (movie != null)
                await _movieRepository.AddOrUpdateMovieAsync(cancellationToken, movie);
            else
                throw new KeyNotFoundException(
                    $"Movie with title '{searchRequest.Title}\\{searchRequest.Year}' not found.");
        }

        return movie;
    }

    public async Task<Movie> CreateMovieReviewAsync(CancellationToken cancellationToken, string imdbId,
        string userOpinion, byte userRating)
    {
        var movie = await GetMovieByImdbIdAsync(cancellationToken, imdbId);
        if (movie == null) throw new KeyNotFoundException($"Movie with IMDb ID {imdbId} not found.");

        movie.AddReview(userOpinion, userRating);

        await _movieRepository.AddOrUpdateMovieAsync(cancellationToken, movie);
        return movie;
    }

    public async Task<Movie> CreateMovieReviewAsync(CancellationToken cancellationToken, CreateMovieReviewDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        var movie = await GetMovieByImdbIdAsync(cancellationToken, dto.ImdbId);
        if (movie == null) throw new KeyNotFoundException($"Movie with IMDb ID {dto.ImdbId} not found.");

        movie.AddReview(dto.UserOpinion, dto.UserRating);

        await _movieRepository.AddOrUpdateMovieAsync(cancellationToken, movie);
        return movie;
    }
}