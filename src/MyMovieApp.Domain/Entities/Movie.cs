namespace MyMovieApp.Domain.Entities;

public class Movie
{
    private Movie(string imdbId, string title, short year)
    {
        ImdbId = imdbId;
        Title = title;
        Year = year;
    }

    public string ImdbId { get; private set; }
    public string Title { get; private set; }
    public short Year { get; private set; }
    public string Genre { get; set; } = default!;
    public string Director { get; set; } = default!;
    public string ImdbRating { get; set; } = default!;
    public string Plot { get; set; } = default!;

    public virtual List<Review> Reviews { get; set; } = new();

    public virtual List<Actor> Actor { get; set; } = new();

    public static Movie Create(string imdbId, string title, short year)
    {
        // Business rule: validate inputs
        ArgumentException.ThrowIfNullOrEmpty(imdbId, nameof(imdbId));
        ArgumentException.ThrowIfNullOrEmpty(title, nameof(title));

        // Business rule: First film: Constants.FirstYearMovie.
        ArgumentOutOfRangeException.ThrowIfLessThan(year, Constants.FirstYearMovie, nameof(year));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(year, DateTime.Today.Year + 1, nameof(year));

        return new Movie(imdbId, title, year);
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