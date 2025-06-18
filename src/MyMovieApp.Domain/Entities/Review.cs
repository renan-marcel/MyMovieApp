using System.Text.Json.Serialization;

namespace MyMovieApp.Domain.Entities;

public class Review
{
    internal Review(string userOpinion, int userRating, string imdbId)
    {
        // Business rule: validate IMDb ID
        ArgumentException.ThrowIfNullOrEmpty(imdbId, imdbId);

        // Business rule: validate user rating
        ArgumentOutOfRangeException.ThrowIfLessThan(userRating, 1, nameof(userRating));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(userRating, 10, nameof(userRating));

        // Business rule: validate user opinion
        ArgumentException.ThrowIfNullOrEmpty(userOpinion, userOpinion);
        if (userOpinion.Length < 10 || userOpinion.Length > 500)
            throw new ArgumentException("User opinion must be between 10 and 500 characters.", nameof(userOpinion));

        UserOpinion = userOpinion;
        UserRating = userRating;
        ImdbId = imdbId;
        Id = Guid.NewGuid();
    }

    [JsonIgnore] public Guid Id { get; private set; }
    public string UserOpinion { get; private set; }
    public int UserRating { get; private set; }

    [JsonIgnore] public string ImdbId { get; private set; }

    [JsonIgnore] public virtual Movie? Movie { get; set; }

    // Factory method to create a review
    public static Review Create(string userOpinion, int userRating, string imdbId)
    {
        return new Review(userOpinion, userRating, imdbId);
    }
}