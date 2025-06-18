using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Tests;
[TestFixture]
public class ReviewTests
{
    [Test]
    public void Create_WithValidParameters_ShouldCreateReview()
    {
        // Arrange  
        var userOpinion = "This is a great movie!";
        var userRating = 9;
        var imdbId = "tt0111161";

        // Act  
        var review = Review.Create(userOpinion, userRating, imdbId);

        // Assert  
        Assert.That(review, Is.Not.Null);
        Assert.That(review.UserOpinion, Is.EqualTo(userOpinion));
        Assert.That(review.UserRating, Is.EqualTo(userRating));
        Assert.That(review.ImdbId, Is.EqualTo(imdbId));
        Assert.That(review.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    [TestCase("This is a great movie!", 0, "tt0111161")]
    [TestCase("This is a great movie!", 11, "tt0111161")]
    [TestCase("This is a great movie!", 9, "")]
    [TestCase("", 9, "tt0111161")]
    [TestCase("Short", 9, "tt0111161")] // Opinion too short  
    [TestCase("This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit. This is a very long opinion that exceeds the maximum character limit.", 9, "tt0111161")] // Opinion too long  
    public void Create_WithInvalidParameters_ShouldThrowArgumentExceptionOrOutOfRangeException(string userOpinion, int userRating, string imdbId)
    {
        // Act & Assert  
        if (string.IsNullOrEmpty(userOpinion) || string.IsNullOrEmpty(imdbId))
        {
            Assert.Throws<ArgumentException>(() => Review.Create(userOpinion, userRating, imdbId));
        }
        else if (userRating < 1 || userRating > 10 || string.IsNullOrEmpty(userOpinion))
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(userOpinion, userRating, imdbId));
        }
        else
        {
            Assert.Throws<ArgumentException>(() => Review.Create(userOpinion, userRating, imdbId));
        }
    }
}

