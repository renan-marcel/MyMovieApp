namespace MyMovieApp.Domain.Entities;

public class Movie
{
    private Movie(string imdbId, string title, int year)
    {
        ImdbId = imdbId;
        Title = title;
        Year = year;
    }

    public string ImdbId { get; private set; }
    public string Title { get; private set; }
    public int Year { get; private set; }
    public string Genre { get; set; } = default!;
    public string Director { get; set; } = default!;
    public string ImdbRating { get; set; } = default!;
    public string Plot { get; set; } = default!;

    public virtual List<Review> Reviews { get; set; } = new();

    public virtual List<Actor> Actor { get; set; } = new();

    public static Movie Create(string imdbId, string title, string? year)
    {
        // Business rule: validate inputs
        ArgumentException.ThrowIfNullOrEmpty(nameof(imdbId), imdbId);
        ArgumentException.ThrowIfNullOrEmpty(nameof(title), title);
        ArgumentException.ThrowIfNullOrEmpty(nameof(year), year);
        if (!int.TryParse(year, out var parsedYear))
            throw new ArgumentException("Year must be a valid integer.", nameof(year));

        // Business rule: First film: Constants.FirstYearMovie.
        ArgumentOutOfRangeException.ThrowIfLessThan(parsedYear, Constants.FirstYearMovie, nameof(year));

        return new Movie(imdbId, title, parsedYear);
    }

    public void AddReview(string userOpinion, int userRating)
    {
        var newReview = new Review(userOpinion, userRating, ImdbId);
        Reviews.Add(newReview);
    }

    public void AddNewActor(string name)
    {
        var actor = new Actor(name);
        Actor.Add(actor);
    }
}