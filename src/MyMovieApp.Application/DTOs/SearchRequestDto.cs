using System.ComponentModel.DataAnnotations;

namespace MyMovieApp.Application.DTOs;

public record SearchRequestDto(
    [Required] string Title,
    short? Year = null);