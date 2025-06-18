using Bogus;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Tests;

[TestFixture]
public class MovieTests
{
    private Faker _faker = null!;

    [SetUp]
    public void Setup()
    {
        _faker = new Faker();
    }

    [Test]
    public void Create_Movie_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        var imdbId = _faker.Random.AlphaNumeric(10);
        var title = _faker.Lorem.Sentence(3);
        var year = _faker.Random.Short(1888, (short)DateTime.Now.Year);

        // Act
        var movie = Movie.Create(imdbId, title, year);

        // Assert
        Assert.That(movie, Is.Not.Null);
        Assert.That(movie.ImdbId, Is.EqualTo(imdbId));
        Assert.That(movie.Title, Is.EqualTo(title));
        Assert.That(movie.Year, Is.EqualTo(year));
    }

    [Test]
    public void Create_Movie_WithInvalidImdbId_ShouldThrowArgumentException()
    {
        // Arrange
        string? imdbId = null;
        var title = _faker.Lorem.Sentence(3);
        var year = _faker.Random.Short(1888, (short)DateTime.Now.Year);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Movie.Create(imdbId!, title, year));

        imdbId = "";
        Assert.Throws<ArgumentException>(() => Movie.Create(imdbId!, title, year));
    }

    [Test]
    public void Create_Movie_WithInvalidTitle_ShouldThrowArgumentException()
    {
        var imdbId = _faker.Random.AlphaNumeric(10);
        string? title = null;
        var year = _faker.Random.Short(1888,(short) DateTime.Now.Year);

        Assert.Throws<ArgumentNullException>(() => Movie.Create(imdbId, title!, year));

        title = "";
        Assert.Throws<ArgumentException>(() => Movie.Create(imdbId, title!, year));
    }

    [Test]
    public void AddReview_ToMovie_ShouldAddReviewToList()
    {
        var movie = Movie.Create(_faker.Random.AlphaNumeric(10), _faker.Lorem.Sentence(3), _faker.Random.Short(1888, (short)DateTime.Now.Year));
        var userOpinion = _faker.Lorem.Sentence(10);
        var userRating = _faker.Random.Int(1, 10);

        movie.AddReview(userOpinion, userRating);

        Assert.That(movie.Reviews.Count, Is.EqualTo(1));
        Assert.That(movie.Reviews[0].UserOpinion, Is.EqualTo(userOpinion));
        Assert.That(movie.Reviews[0].UserRating, Is.EqualTo(userRating));
    }

    [Test]
    public void AddNewActor_ToMovie_ShouldAddActorToList()
    {
        var movie = Movie.Create(_faker.Random.AlphaNumeric(10), _faker.Lorem.Sentence(3), _faker.Random.Short(1888, (short)DateTime.Now.Year));
        var actorName = _faker.Name.FullName();

        movie.AddNewActor(actorName);

        Assert.That(movie.Actor.Count, Is.EqualTo(1));
        Assert.That(movie.Actor[0].Name, Is.EqualTo(actorName));
    }

    [Test]
    [Repeat(100)]
    public void Create_Movie_WithVariousValidInputs_UsingBogus_ShouldSucceed()
    {
        var imdbId = _faker.Random.AlphaNumeric(_faker.Random.Int(5, 15));
        var title = _faker.Lorem.Sentence(_faker.Random.Int(1, 5));
        var year = _faker.Random.Short(1888,(short) DateTime.Now.Year);

        var movie = Movie.Create(imdbId, title, year);

        Assert.That(movie, Is.Not.Null);
        Assert.That(movie.ImdbId, Is.EqualTo(imdbId));
        Assert.That(movie.Title, Is.EqualTo(title));
        Assert.That(movie.Year, Is.EqualTo(year));
        Assert.That(movie.Reviews.Count == 0, Is.True);
        Assert.That(movie.Actor.Count == 0, Is.True);
    }
}
