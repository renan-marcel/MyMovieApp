using Bogus;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Tests;


[TestFixture]
public class ReviewTests
{
    private Faker _faker = null!;

    [SetUp]
    public void Setup()
    {
        _faker = new Faker();
    }

    [Test]
    public void Create_Review_WithValidInputs_ShouldSucceed()
    {
        // Arrange
        string userOpinion = _faker.Lorem.Sentence(_faker.Random.Int(2, 5)); // Generates a sentence with 2 to 5 words, likely > 10 chars
        if (userOpinion.Length < 10) userOpinion = _faker.Lorem.Letter(10); // Ensure minimum length
        int userRating = _faker.Random.Int(1, 10);

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        // Act
        var review = Review.Create(userOpinion, userRating, imdbId);

        // Assert
        Assert.That(review, Is.Not.Null);
        Assert.That(review.UserOpinion, Is.EqualTo(userOpinion));
        Assert.That(review.UserRating, Is.EqualTo(userRating));
        Assert.That(review.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Create_Review_WithInvalidUserRating_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var userOpinion = _faker.Lorem.Letter(20); // Valid opinion

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        // Test rating less than 1
        var ratingTooLow = _faker.Random.Int(-100, 0);
        Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(userOpinion, ratingTooLow, imdbId));

        // Test rating greater than 10
        var ratingTooHigh = _faker.Random.Int(11, 100);
        Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(userOpinion, ratingTooHigh, imdbId));
    }

    [Test]
    public void Create_Review_WithInvalidUserOpinion_ShouldThrowArgumentException()
    {
        // Arrange
        var userRating = _faker.Random.Int(1, 10); // Valid rating

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        // Test null opinion
        Assert.Throws<ArgumentNullException>(() => Review.Create(null!, userRating, imdbId));

        // Test empty opinion
        Assert.Throws<ArgumentException>(() => Review.Create("", userRating, imdbId));

        // Test opinion too short
        var shortOpinion = _faker.Lorem.Letter(_faker.Random.Int(1, 9));
        Assert.Throws<ArgumentException>(() => Review.Create(shortOpinion, userRating, imdbId));

        // Test opinion too long
        var longOpinion = _faker.Lorem.Letter(_faker.Random.Int(501, 600));
        Assert.Throws<ArgumentException>(() => Review.Create(longOpinion, userRating, imdbId));
    }

    // Using Bogus to generate many test cases for Review.Create
    [Test]
    [Repeat(100)]
    public void Create_Review_WithVariousValidInputs_UsingBogus_ShouldSucceed()
    {
        // Arrange
        // Generate opinion ensuring it's within valid length constraints
        var userOpinion = _faker.Lorem.Sentences(_faker.Random.Int(1, 3));
        if (userOpinion.Length < 10) userOpinion = _faker.Lorem.Letter(10);
        if (userOpinion.Length > 500) userOpinion = userOpinion.Substring(0, 500);

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        var userRating = _faker.Random.Int(1, 10);

        // Act
        var review = Review.Create(userOpinion, userRating, imdbId);

        // Assert
        Assert.That(review, Is.Not.Null);
        Assert.That(review.UserOpinion, Is.EqualTo(userOpinion));
        Assert.That(review.UserRating, Is.EqualTo(userRating));
        Assert.That(review.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    [Repeat(50)]
    public void Create_Review_WithVariousInvalidOpinions_UsingBogus_ShouldThrowArgumentException()
    {
        // Arrange
        var userRating = _faker.Random.Int(1, 10); // Valid rating

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        // Test opinion too short
        var shortOpinion = _faker.Lorem.Letter(_faker.Random.Int(1, 9));
        Assert.Throws<ArgumentException>(() => Review.Create(shortOpinion, userRating, imdbId));

        // Test opinion too long
        var longOpinion = _faker.Lorem.Letter(_faker.Random.Int(501, 700)); // Generate longer string
        Assert.Throws<ArgumentException>(() => Review.Create(longOpinion, userRating, imdbId));
    }

    [Test]
    [Repeat(50)]
    public void Create_Review_WithVariousInvalidRatings_UsingBogus_ShouldThrowArgumentOutOfRangeException()
    {
        // Arrange
        var userOpinion = _faker.Lorem.Letter(20); // Valid opinion

        var imdbId = "tt" + _faker.Random.Number(1000000, 9999999).ToString("D7");

        // Test rating less than 1 or greater than 10
        var invalidRating = _faker.PickRandom(new[] { _faker.Random.Int(-100, 0), _faker.Random.Int(11, 100) });
        Assert.Throws<ArgumentOutOfRangeException>(() => Review.Create(userOpinion, invalidRating, imdbId));
    }
}

