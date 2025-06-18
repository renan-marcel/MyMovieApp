using Bogus;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Domain.Tests;

[TestFixture]
public class ActorTests
{
    private Faker _faker = null!;

    [SetUp]
    public void Setup()
    {
        _faker = new Faker();
    }

    [Test]
    public void Constructor_WithValidName_ShouldCreateActor()
    {
        // Arrange
        var name = _faker.Name.FullName();

        // Act
        var actor = Actor.Create(name);

        // Assert
        Assert.That(actor, Is.Not.Null);
        Assert.That(actor.Name, Is.EqualTo(name));
        Assert.That(actor.Id, Is.Not.EqualTo(Guid.Empty));
    }

    [Test]
    public void Constructor_WithNullName_ShouldThrowArgumentNullException()
    {
        // Arrange
        string? name = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => Actor.Create(name!));
    }

    [Test]
    public void Constructor_WithEmptyName_ShouldThrowArgumentException()
    {
        // Arrange
        var name = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() => Actor.Create(name));
    }

    [Test]
    [Repeat(100)]
    public void Constructor_WithVariousValidNames_UsingBogus_ShouldSucceed()
    {
        var name = _faker.Name.FullName();

        if (string.IsNullOrEmpty(name))
        {
            Assert.Throws<ArgumentException>(() => Actor.Create(name));
        }
        else
        {
            // Act
            var actor = Actor.Create(name);

            // Assert
            Assert.That(actor, Is.Not.Null);
            Assert.That(actor.Name, Is.EqualTo(name));
            Assert.That(actor.Id, Is.Not.EqualTo(Guid.Empty));
        }
    }


    [Test]
    [Repeat(100)]
    public void Constructor_WithVariousEmptyNames_NotShouldSucceed()
    {
        var name = string.Empty;

        Assert.Throws<ArgumentException>(() => Actor.Create(name));
    }
}