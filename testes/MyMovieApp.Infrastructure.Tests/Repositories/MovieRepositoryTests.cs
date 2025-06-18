using Bogus;
using Microsoft.EntityFrameworkCore;
using Moq;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Infrastructure.Data;
using MyMovieApp.Infrastructure.Repositories;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyMovieApp.Infrastructure.Tests.Repositories
{
    [TestFixture]
    public class MovieRepositoryTests
    {
        private DbContextOptions<MoviesDbContext> _dbContextOptions;
        private Mock<IDbContextFactory<MoviesDbContext>> _mockDbContextFactory;
        private MovieRepository _movieRepository;
        private Faker<Actor> _actorFaker;
        private Faker<Review> _reviewFaker;
        private Faker<Movie> _movieFaker;

        [SetUp]
        public void Setup()
        {
            // Each test will use a new InMemory database instance.
            _dbContextOptions = new DbContextOptionsBuilder<MoviesDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            // It's often simpler to have the factory return a new context instance each time,
            // but they will all share the same InMemory database if options are the same.
            // For true isolation per CreateDbContextAsync call if needed by repository logic,
            // the factory itself would need to manage unique DB names or instances.
            // For this setup, each test method gets a unique DB via Guid.NewGuid() in options.
            _mockDbContextFactory = new Mock<IDbContextFactory<MoviesDbContext>>();
            _mockDbContextFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => new MoviesDbContext(_dbContextOptions)); // Creates new context with same DB store

            _movieRepository = new MovieRepository(_mockDbContextFactory.Object);

            // Bogus fakers
            _actorFaker = new Faker<Actor>()
                .CustomInstantiator(f => Actor.Create(f.Name.FullName()));

            _reviewFaker = new Faker<Review>()
                .CustomInstantiator(f => Review.Create(
                    f.Lorem.Sentences(2), // Ensure opinion is long enough
                    f.Random.Int(1, 10),   // Rating 1-10
                    "tt_dummy_id")); // ImdbId will be overridden by Movie faker

            // No 'User' property on Review entity

            _movieFaker = new Faker<Movie>()
                .CustomInstantiator(f => Movie.Create(
                    "tt" + f.Random.ReplaceNumbers("#######"),
                    f.Company.CompanyName() + " " + f.Lorem.Word(),
                    (short)f.Random.Int(MyMovieApp.Domain.Constants.FirstYearMovie, DateTime.Now.Year)))
                .RuleFor(m => m.Genre, f => f.Lorem.Word())
                .RuleFor(m => m.Director, f => f.Name.FullName())
                .RuleFor(m => m.ImdbRating, f => f.Random.Decimal(1, 10).ToString("0.0"))
                .RuleFor(m => m.Plot, f => f.Lorem.Paragraph())
                .RuleFor(m => m.Actor, (f, m) => _actorFaker.Generate(f.Random.Int(1, 3)).ToList()) // Actors added via Movie.AddNewActor or directly if constructor allows
                .RuleFor(m => m.Reviews, (f, m) => {
                    var reviews = new List<Review>();
                    var reviewCount = f.Random.Int(0, 2);
                    for (int i = 0; i < reviewCount; i++)
                    {
                        // Create review associated with this movie's ImdbId
                        reviews.Add(Review.Create(f.Lorem.Sentences(2), f.Random.Int(1,10), m.ImdbId));
                    }
                    return reviews;
                });
        }

        [TearDown]
        public async Task TearDown()
        {
            // Ensure the database is deleted after each test for true isolation,
            // though for InMemory, a new GUID name per test usually suffices.
            // This is more thorough if the DB name wasn't unique per test.
            await using var context = new MoviesDbContext(_dbContextOptions);
            await context.Database.EnsureDeletedAsync();
        }

        [Test]
        public async Task GetByImdbIdAsync_MovieFound_ReturnsMovieWithReviewsAndActors()
        {
            // Arrange
            var fakeMovie = _movieFaker.Generate();
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.Add(fakeMovie);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _movieRepository.GetByImdbIdAsync(CancellationToken.None, fakeMovie.ImdbId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fakeMovie.ImdbId, result.ImdbId);
            Assert.AreEqual(fakeMovie.Title, result.Title);
            Assert.AreEqual(fakeMovie.Actor.Count, result.Actor.Count);
            Assert.AreEqual(fakeMovie.Reviews.Count, result.Reviews.Count);
            if (fakeMovie.Actor.Any())
            {
                Assert.AreEqual(fakeMovie.Actor.First().Name, result.Actor.First().Name);
            }
            if (fakeMovie.Reviews.Any())
            {
                Assert.AreEqual(fakeMovie.Reviews.First().UserOpinion, result.Reviews.First().UserOpinion);
            }
        }

        [Test]
        public async Task GetByImdbIdAsync_MovieNotFound_ReturnsNull()
        {
            // Arrange (empty database)

            // Act
            var result = await _movieRepository.GetByImdbIdAsync(CancellationToken.None, "tt_nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetByTitleAsync_MovieFoundByTitleOnly_ReturnsMovie()
        {
            // Arrange
            var fakeMovie = _movieFaker.Generate();
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.Add(fakeMovie);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _movieRepository.GetByTitleAsync(CancellationToken.None, fakeMovie.Title, null);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fakeMovie.ImdbId, result.ImdbId);
            Assert.AreEqual(fakeMovie.Title, result.Title);
        }

        [Test]
        public async Task GetByTitleAsync_MovieFoundByTitleAndYear_ReturnsMovie()
        {
            // Arrange
            var fakeMovie = _movieFaker.Generate();
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.Add(fakeMovie);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await _movieRepository.GetByTitleAsync(CancellationToken.None, fakeMovie.Title, fakeMovie.Year);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fakeMovie.ImdbId, result.ImdbId);
            Assert.AreEqual(fakeMovie.Title, result.Title);
            Assert.AreEqual(fakeMovie.Year, result.Year);
        }

        [Test]
        public async Task GetByTitleAsync_MovieNotFound_ReturnsNull()
        {
            // Act
            var result = await _movieRepository.GetByTitleAsync(CancellationToken.None, "Non Existent Title", null);
            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task SearchMoviesAsync_ByTitle_ReturnsMatchingMovies()
        {
            // Arrange
            var movie1 = _movieFaker.Clone().RuleFor(m => m.Title, "SearchTarget First Movie").Generate();
            var movie2 = _movieFaker.Clone().RuleFor(m => m.Title, "Another Movie").Generate();
            var movie3 = _movieFaker.Clone().RuleFor(m => m.Title, "SearchTarget Second Movie").Generate();
            var movies = new List<Movie> { movie1, movie2, movie3 };

            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.AddRange(movies);
                await context.SaveChangesAsync();
            }

            // Act
            var results = await _movieRepository.SearchMoviesAsync(CancellationToken.None, "SearchTarget", null);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.Any(m => m.Title == "SearchTarget First Movie"));
            Assert.IsTrue(results.Any(m => m.Title == "SearchTarget Second Movie"));
            // Check eager loading
            var firstResult = results.First(m => m.Title == "SearchTarget First Movie");
            var originalMovie = movies.First(m => m.Title == "SearchTarget First Movie");
            Assert.AreEqual(originalMovie.Actor.Count, firstResult.Actor.Count);
            Assert.AreEqual(originalMovie.Reviews.Count, firstResult.Reviews.Count);
        }

        [Test]
        public async Task SearchMoviesAsync_ByYear_ReturnsMatchingMovies()
        {
            // Arrange
            var movie1 = _movieFaker.Clone().RuleFor(m => m.Year, (short)2000).Generate();
            var movie2 = _movieFaker.Clone().RuleFor(m => m.Year, (short)2001).Generate();
            var movie3 = _movieFaker.Clone().RuleFor(m => m.Year, (short)2000).Generate();
            var movies = new List<Movie> { movie1, movie2, movie3 };
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.AddRange(movies);
                await context.SaveChangesAsync();
            }

            // Act
            var results = await _movieRepository.SearchMoviesAsync(CancellationToken.None, null, 2000);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(2, results.Count());
            Assert.IsTrue(results.All(m => m.Year == 2000));
        }

        [Test]
        public async Task SearchMoviesAsync_ByTitleAndYear_ReturnsMatchingMovies()
        {
            // Arrange
            var movie1 = _movieFaker.Clone().RuleFor(m => m.Title, "SpecificTitle").RuleFor(m => m.Year, (short)2020).Generate();
            var movie2 = _movieFaker.Clone().RuleFor(m => m.Title, "SpecificTitle").RuleFor(m => m.Year, (short)2021).Generate();
            var movie3 = _movieFaker.Clone().RuleFor(m => m.Title, "AnotherTitle").RuleFor(m => m.Year, (short)2020).Generate();
            var movies = new List<Movie> { movie1, movie2, movie3 };
             await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.AddRange(movies);
                await context.SaveChangesAsync();
            }

            // Act
            var results = await _movieRepository.SearchMoviesAsync(CancellationToken.None, "SpecificTitle", 2020);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(1, results.Count());
            Assert.AreEqual("SpecificTitle", results.First().Title);
            Assert.AreEqual(2020, results.First().Year);
        }

        [Test]
        public async Task SearchMoviesAsync_NoResults_ReturnsEmptyList()
        {
            // Act
            var results = await _movieRepository.SearchMoviesAsync(CancellationToken.None, "NonExistent", 9999);
            // Assert
            Assert.IsNotNull(results);
            Assert.IsEmpty(results);
        }

        [Test]
        public async Task AddOrUpdateMovieAsync_AddNewMovie_AddsMovieAndRelations()
        {
            // Arrange
            var newMovie = _movieFaker.Generate();
            // Ensure reviews and actors are new (not associated with a Movie entity yet, which Bogus does by default)

            // Act
            await _movieRepository.AddOrUpdateMovieAsync(CancellationToken.None, newMovie);

            // Assert
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                var addedMovie = await context.Movies
                    .Include(m => m.Actor)
                    .Include(m => m.Reviews)
                    .FirstOrDefaultAsync(m => m.ImdbId == newMovie.ImdbId);

                Assert.IsNotNull(addedMovie);
                Assert.AreEqual(newMovie.Title, addedMovie.Title);
                Assert.AreEqual(newMovie.Actor.Count, addedMovie.Actor.Count);
                Assert.AreEqual(newMovie.Reviews.Count, addedMovie.Reviews.Count);

                foreach (var originalActor in newMovie.Actor)
                {
                    Assert.IsTrue(addedMovie.Actor.Any(a => a.Name == originalActor.Name));
                }
                foreach (var originalReview in newMovie.Reviews)
                {
                    Assert.IsTrue(addedMovie.Reviews.Any(r => r.UserOpinion == originalReview.UserOpinion && r.UserRating == originalReview.UserRating));
                }
            }
        }

        [Test]
        public async Task AddOrUpdateMovieAsync_UpdateExistingMovie_UpdatesPropertiesAndAddsNewRelations()
        {
            // Arrange: Seed an existing movie
            var existingMovie = _movieFaker.Generate();
            existingMovie.Reviews.Clear(); // Start with no reviews for simplicity in this part
            existingMovie.Actor.Clear();   // Start with no actors

            var initialReview = Review.Create("Initial Opinion sufficient length", 5, existingMovie.ImdbId);
            existingMovie.Reviews.Add(initialReview);

            var initialActor = Actor.Create("Initial Actor");
            existingMovie.Actor.Add(initialActor);

            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                context.Movies.Add(existingMovie);
                await context.SaveChangesAsync();
            }

            // Create an updated version of the movie
            var updatedMovieData = Movie.Create(existingMovie.ImdbId, "Updated Title", existingMovie.Year);
            updatedMovieData.Plot = "Updated Plot"; // Plot is settable

            // Add one new review
            var newReview = Review.Create("This is a brand new review, long enough.", 4, existingMovie.ImdbId);
            updatedMovieData.Reviews.Add(newReview);

            // Add one new actor
            var newActor = Actor.Create("Brand New Actor");
            updatedMovieData.Actor.Add(newActor);


            // Act
            await _movieRepository.AddOrUpdateMovieAsync(CancellationToken.None, updatedMovieData);

            // Assert
            await using (var context = new MoviesDbContext(_dbContextOptions))
            {
                var movieFromDb = await context.Movies
                    .Include(m => m.Actor)
                    .Include(m => m.Reviews)
                    .FirstOrDefaultAsync(m => m.ImdbId == existingMovie.ImdbId);

                Assert.IsNotNull(movieFromDb);
                Assert.AreEqual("Updated Title", movieFromDb.Title);
                Assert.AreEqual("Updated Plot", movieFromDb.Plot);

                // Should have original actor + new actor = 2
                // The current AddOrUpdateMovieAsync in MovieRepository only adds new actors if they are not already tracked
                // or by name. The provided code for MovieRepository has a simple AddRange for actors.
                // Let's assume it merges based on names or adds if not present.
                // The test here adds one initial actor, then one new actor to updatedMovieData.
                // The repository logic for actors is: it clears existing actors and adds all from updatedMovieData.
                Assert.AreEqual(updatedMovieData.Actor.Count, movieFromDb.Actor.Count, "Actor count mismatch.");
                Assert.IsTrue(movieFromDb.Actor.Any(a => a.Name == newActor.Name), "New actor was not added.");
                Assert.IsFalse(movieFromDb.Actor.Any(a => a.Name == initialActor.Name), "Initial actor should have been removed by current repo logic.");


                // Should have initial review + new review = 2 (if new review was correctly added)
                // The logic `movie.Reviews.Where(e => e.Movie is null)` is for adding new reviews
                // that are part of the `updatedMovieData.Reviews` collection.
                Assert.AreEqual(1 + 1, movieFromDb.Reviews.Count, "Review count mismatch.");
                Assert.IsTrue(movieFromDb.Reviews.Any(r => r.UserOpinion == "This is a brand new review"), "New review was not added.");
                Assert.IsTrue(movieFromDb.Reviews.Any(r => r.UserOpinion == initialReview.UserOpinion), "Initial review was removed.");
            }
        }
    }
}
