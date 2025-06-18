using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Tests;

[TestFixture]
public class MovieTests
{
    [Test]
    public void Create_WithValidParameters_ShouldCreateMovie()
    {
        // Arrange
        var imdbId = "tt0111161";
        var title = "The Shawshank Redemption";
        short year = 1994;

        // Act
        var movie = Movie.Create(imdbId, title, year);

        // Assert
        Assert.That(movie, Is.Not.Null);
        Assert.That(movie.Title, Is.EqualTo(title));
        Assert.That(movie.ImdbId, Is.EqualTo(imdbId));
        Assert.That(movie.Year, Is.EqualTo(year)); 
    }
}
