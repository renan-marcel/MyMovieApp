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

    public async Task<Movie> GetMovieByImdbIdAsync(string imdbId, CancellationToken cancellationToken)
    {
        var movie = await _movieRepository.GetByImdbIdAsync(imdbId, cancellationToken);

        if (movie == null)
        {
            // Fetch from OMDb if not in local database
            movie = await _omdbProvider.GetMovieByImdbIdAsync(imdbId);
            if (movie != null)
            {
                await _movieRepository.AddOrUpdateMovieAsync(movie, cancellationToken);
            }
        }

        return movie;
    }

    public async Task<List<Movie>> SearchMoviesAsync(string title, int? year, CancellationToken cancellationToken)
    {
        return await _movieRepository.SearchMoviesAsync(title, year, cancellationToken);
    }

    public async Task<Movie> CreateMovieReviewAsync(string imdbId, string userOpinion, int userRating, CancellationToken cancellationToken)
    {
        var movie = await GetMovieByImdbIdAsync(imdbId, cancellationToken);
        if (movie == null)
        {
            throw new KeyNotFoundException($"Movie with IMDb ID {imdbId} not found.");
        }

        var review =  Review.Create(userOpinion, userRating);
        movie.Reviews.Add(review);

        await _movieRepository.AddOrUpdateMovieAsync(movie, cancellationToken);
        return movie;
    }

    public async Task<Movie> CreateMovieReviewAsync(CreateMovieReviewDto dto, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto, nameof(dto));

        var movie = await GetMovieByImdbIdAsync(dto.ImdbId, cancellationToken);
        if (movie == null)
        {
            throw new KeyNotFoundException($"Movie with IMDb ID {dto.ImdbId} not found.");
        }

        var review = Review.Create(dto.UserOpinion, dto.UserRating);
        movie.Reviews.Add(review);

        await _movieRepository.AddOrUpdateMovieAsync(movie, cancellationToken);
        return movie;
    }
}