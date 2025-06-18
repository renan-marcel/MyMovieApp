using System.Text.Json.Serialization;

namespace MyMovieApp.Infrastructure.External.Models;

public class OmdbResponse
{
    [JsonPropertyName("Title")] public string Title { get; set; }

    [JsonPropertyName("Year")] public string? Year { get; set; }

    [JsonPropertyName("imdbID")] public string ImdbID { get; set; }

    [JsonPropertyName("Genre")] public string? Genre { get; set; }

    [JsonPropertyName("Director")] public string? Director { get; set; }

    [JsonPropertyName("Actors")]
    public string? Actors { get; set; } // Omdb retorna atores como uma única string separada por vírgulas.

    [JsonPropertyName("Plot")] public string? Plot { get; set; }

    [JsonPropertyName("imdbRating")] public string? ImdbRating { get; set; }

    // Omdb retorna "True" ou "False" em uma propriedade Response
    [JsonPropertyName("Response")] public string Response { get; set; } = "False";

    [JsonPropertyName("Error")] public string? Error { get; set; }
}