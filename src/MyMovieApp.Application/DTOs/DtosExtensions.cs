using MyMovieApp.Domain;

namespace MyMovieApp.Application.DTOs;

public static class DtosExtensions
{
    public static bool isValid(this SearchRequestDto requestDto)
    {
        return !string.IsNullOrWhiteSpace(requestDto.Title) || (requestDto.Year.HasValue &&
                                                                (requestDto.Year < Constants.FirstYearMovie ||
                                                                 requestDto.Year > DateTime.Now.Year));
    }
}