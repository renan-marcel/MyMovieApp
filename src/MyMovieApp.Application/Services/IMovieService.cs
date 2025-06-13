using MyMovieApp.Application.DTOs;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Application.Interfaces;
public interface IMovieService
{
    Task<Movie> GetMovieByImdbIdAsync(string imdbId, CancellationToken cancellationToken);
    Task<List<Movie>> SearchMoviesAsync(string title, int? year, CancellationToken cancellationToken);
    Task<Movie> CreateMovieReviewAsync(string imdbId, string userOpinion, int userRating, CancellationToken cancellationToken);
    Task<Movie> CreateMovieReviewAsync(CreateMovieReviewDto dto, CancellationToken cancellationToken);
}