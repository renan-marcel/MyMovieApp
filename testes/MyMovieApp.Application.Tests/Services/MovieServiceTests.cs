using Bogus;
using Moq;
using MyMovieApp.Application.DTOs;
using MyMovieApp.Application.Services;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.External; // For IOmdbMovieProvider
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging; // For ILogger
using System; // For Func
using MyMovieApp.Domain; // Added for Constants

namespace MyMovieApp.Application.Tests.Services
{
    [TestFixture]
    public class MovieServiceTests
    {
        private Mock<IMovieRepository> _mockMovieRepository;
        private Mock<IOmdbMovieProvider> _mockOmdbMovieProvider;
        // private Mock<ILogger<MovieService>> _mockLogger; // Logger not in constructor
        private MovieService _movieService;
        private Faker<Movie> _movieFaker;
        private Faker<Review> _reviewFaker;
        private Faker<Actor> _actorFaker;
        private Faker<CreateMovieReviewDto> _createReviewDtoFaker;

        [SetUp]
        public void Setup()
        {
            _mockMovieRepository = new Mock<IMovieRepository>();
            _mockOmdbMovieProvider = new Mock<IOmdbMovieProvider>();
            // _mockLogger = new Mock<ILogger<MovieService>>(); // Logger not in constructor
            _movieService = new MovieService(
                _mockMovieRepository.Object,
                _mockOmdbMovieProvider.Object
                // _mockLogger.Object // Logger not in constructor
            );

            _actorFaker = new Faker<Actor>()
                .CustomInstantiator(f => Actor.Create(f.Random.String2(10))); // Name

            _reviewFaker = new Faker<Review>()
                .CustomInstantiator(f => Review.Create(
                    f.Random.String2(15), // UserOpinion (min 10 chars for Review entity)
                    (byte)f.Random.Int(1, 10), // UserRating (byte, 1-10)
                    $"tt{f.Random.Number(1000000, 9999999)}" // ImdbId (string)
                ));

            _movieFaker = new Faker<Movie>()
                .CustomInstantiator(f => Movie.Create(
                    $"tt{f.Random.Number(1000000, 9999999)}", // ImdbId
                    f.Random.String2(10), // Title
                    (short)f.Random.Int(Constants.FirstYearMovie, DateTime.Now.Year) // Year (short)
                ))
                .RuleFor(m => m.Genre, f => f.Random.String2(10))
                .RuleFor(m => m.Director, f => f.Random.String2(10))
                .RuleFor(m => m.ImdbRating, f => $"{f.Random.Byte(1,9)}.{f.Random.Byte(0,9)}")
                .RuleFor(m => m.Plot, f => f.Random.String2(30))
                // No Synopsis, PosterUrl on Movie entity
                // Actors collection is 'Actor' (note casing)
                .RuleFor(m => m.Actor, f => _actorFaker.Generate(f.Random.Int(1, 3)).ToList())
                .RuleFor(m => m.Reviews, f =>
                    {
                        // Reviews need to be created with the Movie's ImdbId if they are to be linked.
                        // However, Movie.Create doesn't expose ImdbId immediately for faker context.
                        // For simplicity now, Review faker makes its own ImdbId.
                        // Proper linking would require post-generation assignment or more complex faker setup.
                        return _reviewFaker.Generate(f.Random.Int(0, 2)).ToList();
                    }
                );

            _createReviewDtoFaker = new Faker<CreateMovieReviewDto>()
                .RuleFor(dto => dto.ImdbId, f => $"tt{f.Random.Number(1000000, 9999999)}")
                .RuleFor(dto => dto.UserOpinion, f => f.Random.String2(15)) // UserOpinion (min 5 for DTO, min 10 for Entity)
                .RuleFor(dto => dto.UserRating, f => f.Random.Int(1, 10));
        }

        // --- GetMovieByTitleAsync Tests (adapted from SearchMoviesAsync) ---
        [Test]
        public async Task GetMovieByTitleAsync_MovieFoundInRepository_ShouldReturnMovieFromRepository()
        {
            // Arrange
            var movieFromRepo = _movieFaker.Generate(); // Faker now uses Movie.Create
            var searchDto = new SearchRequestDto(movieFromRepo.Title, movieFromRepo.Year);

            _mockMovieRepository.Setup(r => r.GetByTitleAsync(It.IsAny<CancellationToken>(), searchDto.Title, searchDto.Year))
                                .ReturnsAsync(movieFromRepo);

            // Act
            var result = await _movieService.GetMovieByTitleAsync(CancellationToken.None, searchDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo(movieFromRepo)); // Expecting a Movie entity
            _mockOmdbMovieProvider.Verify(o => o.GetMovieByTitleAsync(It.IsAny<string>(), It.IsAny<short?>()), Times.Never);
        }

        [Test]
        public async Task GetMovieByTitleAsync_MovieNotFoundInRepo_FoundInOmdb_ShouldFetchAndAdd_AndReturnMovie()
        {
            // Arrange
            var searchDto = new SearchRequestDto("New Movie From OMDb", (short)2022);
            // Use Movie.Create directly for movieFromOmdb to ensure Title and Year match searchDto
            var movieFromOmdb = Movie.Create(
                $"tt{new Faker().Random.Number(1000000, 9999999)}",
                searchDto.Title,
                searchDto.Year.Value
            );

            _mockMovieRepository.Setup(r => r.GetByTitleAsync(It.IsAny<CancellationToken>(), searchDto.Title, searchDto.Year))
                                .ReturnsAsync((Movie)null); // Not in repo
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByTitleAsync(searchDto.Title, searchDto.Year))
                                  .ReturnsAsync(movieFromOmdb);
            _mockMovieRepository.Setup(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()))
                                .Returns(Task.CompletedTask); // Use AddOrUpdateMovieAsync

            // Act
            var result = await _movieService.GetMovieByTitleAsync(CancellationToken.None, searchDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Title, Is.EqualTo(searchDto.Title));
            Assert.That(result.ImdbId, Is.EqualTo(movieFromOmdb.ImdbId));
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.Is<Movie>(m => m.ImdbId == movieFromOmdb.ImdbId)), Times.Once);
        }

        [Test]
        public async Task GetMovieByTitleAsync_MovieNotFoundInRepo_OmdbReturnsData_AddOrUpdateCalled()
        {
            // This test replaces SearchMoviesAsync_MovieNotFoundInRepo_FoundInOmdb_ButAlreadyInDbByImdbId_ShouldNotAddAgain
            // The "not add again" logic is now assumed to be handled by AddOrUpdateMovieAsync.
            // This test ensures that if OMDb provides a movie, AddOrUpdateMovieAsync is called.
            // Arrange
            var searchDto = new SearchRequestDto("Existing OMDB Title", (short)2021);
            var movieFromOmdb = Movie.Create(
                $"tt{new Faker().Random.Number(1000000, 9999999)}",
                searchDto.Title,
                searchDto.Year.Value
            );

            _mockMovieRepository.Setup(r => r.GetByTitleAsync(It.IsAny<CancellationToken>(), searchDto.Title, searchDto.Year))
                                .ReturnsAsync((Movie)null);
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByTitleAsync(searchDto.Title, searchDto.Year))
                                  .ReturnsAsync(movieFromOmdb);
            _mockMovieRepository.Setup(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()))
                                .Returns(Task.CompletedTask);

            // Act
            var result = await _movieService.GetMovieByTitleAsync(CancellationToken.None, searchDto);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ImdbId, Is.EqualTo(movieFromOmdb.ImdbId));
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.Is<Movie>(m => m.ImdbId == movieFromOmdb.ImdbId)), Times.Once);
        }

        [Test]
        public async Task GetMovieByTitleAsync_NotFoundInRepoAndOmdb_ShouldThrowException()
        {
            // Arrange
            var searchDto = new SearchRequestDto("Unknown Movie Title", (short)2020);
            _mockMovieRepository.Setup(r => r.GetByTitleAsync(It.IsAny<CancellationToken>(), searchDto.Title, searchDto.Year))
                                .ReturnsAsync((Movie)null);
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByTitleAsync(searchDto.Title, searchDto.Year))
                                  .ReturnsAsync((Movie)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _movieService.GetMovieByTitleAsync(CancellationToken.None, searchDto));
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()), Times.Never);
        }

        // --- GetMovieByImdbIdAsync Tests (adapted from GetMovieByIdAsync) ---
        [Test]
        public async Task GetMovieByImdbIdAsync_MovieExistsInRepository_ShouldReturnMovie()
        {
            // Arrange
            var movieFromRepo = _movieFaker.Generate(); // Has ImdbId from faker
            _mockMovieRepository.Setup(r => r.GetByImdbIdAsync(It.IsAny<CancellationToken>(), movieFromRepo.ImdbId))
                                .ReturnsAsync(movieFromRepo);

            // Act
            var result = await _movieService.GetMovieByImdbIdAsync(CancellationToken.None, movieFromRepo.ImdbId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ImdbId, Is.EqualTo(movieFromRepo.ImdbId));
            Assert.That(result.Title, Is.EqualTo(movieFromRepo.Title));
            _mockOmdbMovieProvider.Verify(o => o.GetMovieByImdbIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_MovieNotFoundInRepo_FoundInOmdb_ShouldFetchAndAdd_ReturnMovie()
        {
            // Arrange
            var imdbId = $"tt{new Faker().Random.Number(1000000, 9999999)}";
            var movieFromOmdb = Movie.Create(imdbId, "Fetched From OMDb", 2023);

            _mockMovieRepository.Setup(r => r.GetByImdbIdAsync(It.IsAny<CancellationToken>(), imdbId))
                                .ReturnsAsync((Movie)null); // Not in repo
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByImdbIdAsync(imdbId))
                                  .ReturnsAsync(movieFromOmdb);
            _mockMovieRepository.Setup(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()))
                                .Returns(Task.CompletedTask);

            // Act
            var result = await _movieService.GetMovieByImdbIdAsync(CancellationToken.None, imdbId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.ImdbId, Is.EqualTo(imdbId));
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.Is<Movie>(m => m.ImdbId == imdbId)), Times.Once);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_MovieDoesNotExistInRepoOrOmdb_ShouldThrowException()
        {
            // Arrange
            var movieImdbId = $"tt{new Faker().Random.Number(1000000, 9999999)}";
            _mockMovieRepository.Setup(r => r.GetByImdbIdAsync(It.IsAny<CancellationToken>(), movieImdbId)).ReturnsAsync((Movie)null);
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByImdbIdAsync(movieImdbId)).ReturnsAsync((Movie)null);

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _movieService.GetMovieByImdbIdAsync(CancellationToken.None, movieImdbId));
        }

        // --- CreateMovieReviewAsync Tests (adapted from AddMovieReviewAsync) ---
        [Test]
        public async Task CreateMovieReviewAsync_MovieExists_ShouldAddReviewAndReturnMovie()
        {
            // Arrange
            var reviewDto = _createReviewDtoFaker.Generate(); // DTO has ImdbId, UserOpinion, UserRating
            // Ensure movie exists for the review
            var movie = Movie.Create(reviewDto.ImdbId, "Existing Movie for Review", (short)DateTime.Now.Year);

            _mockMovieRepository.Setup(r => r.GetByImdbIdAsync(It.IsAny<CancellationToken>(), reviewDto.ImdbId))
                                .ReturnsAsync(movie);
            _mockMovieRepository.Setup(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()))
                                .Returns(Task.CompletedTask);

            // Act
            var resultMovie = await _movieService.CreateMovieReviewAsync(CancellationToken.None, reviewDto);

            // Assert
            Assert.That(resultMovie, Is.Not.Null);
            Assert.That(resultMovie.ImdbId, Is.EqualTo(reviewDto.ImdbId));
            Assert.That(resultMovie.Reviews, Is.Not.Empty);
            Assert.That(resultMovie.Reviews.Any(r => r.UserOpinion == reviewDto.UserOpinion && r.UserRating == reviewDto.UserRating), Is.True);
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.Is<Movie>(m => m.ImdbId == reviewDto.ImdbId)), Times.Once);
        }

        [Test]
        public async Task CreateMovieReviewAsync_MovieDoesNotExist_ShouldThrowException()
        {
            // Arrange
            var reviewDto = _createReviewDtoFaker.Generate();
            _mockMovieRepository.Setup(r => r.GetByImdbIdAsync(It.IsAny<CancellationToken>(), reviewDto.ImdbId))
                                .ReturnsAsync((Movie)null); // Movie not found by GetByImdbIdAsync
            // Service's GetMovieByImdbIdAsync will then try OMDB
            _mockOmdbMovieProvider.Setup(o => o.GetMovieByImdbIdAsync(reviewDto.ImdbId))
                                  .ReturnsAsync((Movie)null); // Also not found in OMDB

            // Act & Assert
            Assert.ThrowsAsync<KeyNotFoundException>(async () => await _movieService.CreateMovieReviewAsync(CancellationToken.None, reviewDto));
            _mockMovieRepository.Verify(r => r.AddOrUpdateMovieAsync(It.IsAny<CancellationToken>(), It.IsAny<Movie>()), Times.Never);
        }

        // Note: All tests for GetMovieDetailsAsync and SearchMoviesAsync_EmptySearchTerm_ShouldReturnEmptyList
        // have been removed as those test cases / service methods are no longer applicable with the
        // current IMovieService interface and entity definitions.
    }
}
