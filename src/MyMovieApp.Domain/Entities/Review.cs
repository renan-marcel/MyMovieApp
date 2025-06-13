namespace MyMovieApp.Domain.Entities;
public class Review
{
    public Guid Id { get; private set; }
    public string UserOpinion { get; private set; }
    public int UserRating { get; private set; }

    internal Review(string userOpinion, int userRating)
    {
        // Business rule: validate user rating
        ArgumentOutOfRangeException.ThrowIfLessThan(userRating, 1, nameof(userRating));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(userRating, 10, nameof(userRating));

        // Business rule: validate user opinion
        ArgumentException.ThrowIfNullOrEmpty(nameof(userOpinion), userOpinion);
        if (userOpinion.Length < 10 || userOpinion.Length > 500)
            throw new ArgumentException("User opinion must be between 10 and 500 characters.", nameof(userOpinion));

        Id = Guid.NewGuid();
        UserOpinion = userOpinion;
        UserRating = userRating;
    }

    // Factory method to create a review
    public static Review Create(string userOpinion, int userRating)
    {
        return new Review(userOpinion, userRating);
    }
}