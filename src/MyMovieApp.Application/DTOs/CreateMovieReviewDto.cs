using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace MyMovieApp.Application.DTOs;

public class CreateMovieReviewDto
{
    [Required]
    [JsonPropertyName("imdb_id")]
    public string ImdbId { get; set; }

    [Required]
    [JsonPropertyName("user_opinion")]
    [StringLength(500, MinimumLength = 5)]
    public string UserOpinion { get; set; }

    [Required]
    [Range(1, 10)]
    [JsonPropertyName("user_rating")]
    public int UserRating { get; set; }
}