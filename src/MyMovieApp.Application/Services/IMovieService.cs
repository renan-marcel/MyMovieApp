using MyMovieApp.Application.DTOs;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Application.Interfaces;

public interface IMovieService
{
    Task<Movie> GetMovieByImdbIdAsync(CancellationToken cancellationToken, string imdbId);

    Task<Movie> GetMovieByTitleAsync(CancellationToken cancellationToken, SearchRequestDto searchRequest);

    Task<Movie> CreateMovieReviewAsync(CancellationToken cancellationToken, string imdbId, string userOpinion,
        byte userRating);

    Task<Movie> CreateMovieReviewAsync(CancellationToken cancellationToken, CreateMovieReviewDto dto);
}