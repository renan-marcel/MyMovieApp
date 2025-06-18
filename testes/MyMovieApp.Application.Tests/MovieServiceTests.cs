using Moq;
using Bogus;
using MyMovieApp.Application.Services;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Domain.Interfaces;
using MyMovieApp.Infrastructure.External;
using MyMovieApp.Application.DTOs; 

namespace MyMovieApp.Application.Tests.Services
{
    [TestFixture]
    public class MovieServiceTests
    {
        private Mock<IMovieRepository> _mockMovieRepository;
        private Mock<IOmdbMovieProvider> _mockOmdbProvider;
        private MovieService _movieService;

        [SetUp]
        public void Setup()
        {
            _mockMovieRepository = new Mock<IMovieRepository>();
            _mockOmdbProvider = new Mock<IOmdbMovieProvider>();
            _movieService = new MovieService(_mockMovieRepository.Object, _mockOmdbProvider.Object);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_MovieExistsInRepository_ReturnsMovieAndDoesNotCallOmdb()
        {
            var imdbId = "tt1234567";
            var fakeMovie = new Faker<Movie>()
                .CustomInstantiator(f => Movie.Create(imdbId, f.Lorem.Sentence(10), f.Random.Int(1900, DateTime.Now.Year).ToString()))
                .RuleFor(m => m.ImdbId, imdbId) // This ensures the ImdbId is exactly what's expected for the test
                .RuleFor(m => m.Title, f => f.Lorem.Sentence())
                .RuleFor(m => m.Year, f => f.Random.Int(1900, 2024)) // Year is int
                .Generate();

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovie);

            var result = await _movieService.GetMovieByImdbIdAsync(imdbId, default);

            Assert.That(result, Is.EqualTo(fakeMovie));
            _mockOmdbProvider.Verify(omdb => omdb.GetMovieByImdbIdAsync(It.IsAny<string>()), Times.Never); // Removed CancellationToken
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_MovieNotInRepository_FoundByOmdb_ReturnsMovieAndAddsToRepository()
        {
            var imdbId = "tt7654321";
            var fakeOmdbMovie = new Faker<Movie>()
                .CustomInstantiator(f => Movie.Create(imdbId, f.Lorem.Sentence(10), f.Random.Int(1900, DateTime.Now.Year).ToString()))
                .RuleFor(m => m.ImdbId, imdbId) 
                .RuleFor(m => m.Title, f => f.Lorem.Sentence())
                .RuleFor(m => m.Year, f => f.Random.Int(1900, 2024))
                .Generate();

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Movie)null);
            _mockOmdbProvider.Setup(omdb => omdb.GetMovieByImdbIdAsync(imdbId)) // Removed CancellationToken
                .ReturnsAsync(fakeOmdbMovie);
            _mockMovieRepository.Setup(repo => repo.AddOrUpdateMovieAsync(fakeOmdbMovie, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            
            var result = await _movieService.GetMovieByImdbIdAsync(imdbId, default);

            Assert.That(result, Is.EqualTo(fakeOmdbMovie));
            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockOmdbProvider.Verify(omdb => omdb.GetMovieByImdbIdAsync(imdbId), Times.Once); // Removed CancellationToken
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(fakeOmdbMovie, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_MovieNotInRepository_NotFoundByOmdb_ReturnsNull()
        {
            var imdbId = "tt0000000";
            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Movie)null);
            _mockOmdbProvider.Setup(omdb => omdb.GetMovieByImdbIdAsync(imdbId)) // Removed CancellationToken
                .ReturnsAsync((Movie)null);

            var result = await _movieService.GetMovieByImdbIdAsync(imdbId, default);

            Assert.That(result, Is.Null);
            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockOmdbProvider.Verify(omdb => omdb.GetMovieByImdbIdAsync(imdbId), Times.Once); // Removed CancellationToken
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        private Faker<Movie> GetMovieFaker(string imdbIdSeed = null)
        {
            return new Faker<Movie>()
                .CustomInstantiator(f => Movie.Create(
                    imdbIdSeed ?? f.Random.Replace("tt#######"),
                    f.Lorem.Sentence(5),
                    f.Random.Int(1900, DateTime.Now.Year).ToString()
                ))
                .RuleFor(m => m.ImdbId, (f, m) => imdbIdSeed ?? m.ImdbId) // Ensure ImdbId is consistent if provided
                .RuleFor(m => m.Title, f => f.Lorem.Sentence(5))
                .RuleFor(m => m.Year, f => f.Random.Int(1900, DateTime.Now.Year))
                .RuleFor(m => m.Genre, f => f.Lorem.Word())
                .RuleFor(m => m.Director, f => f.Name.FullName())
                .RuleFor(m => m.Plot, f => f.Lorem.Paragraph());
        }

        [Test]
        public async Task SearchMoviesAsync_WithTitleAndYear_MoviesFound()
        {
            var title = "Inception";
            var year = 2010;
            var fakeMovies = GetMovieFaker().Generate(3);

            _mockMovieRepository.Setup(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovies);

            var result = await _movieService.SearchMoviesAsync(title, year, default);

            Assert.That(result, Is.EqualTo(fakeMovies));
            _mockMovieRepository.Verify(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SearchMoviesAsync_WithTitleOnly_MoviesFound()
        {
            var title = "Avatar";
            int? year = null;
            var fakeMovies = GetMovieFaker().Generate(2);

            _mockMovieRepository.Setup(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovies);

            var result = await _movieService.SearchMoviesAsync(title, year, default);

            Assert.That(result, Is.EqualTo(fakeMovies));
            _mockMovieRepository.Verify(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SearchMoviesAsync_WithYearOnly_MoviesFound()
        {
            string title = null;
            var year = 2020;
            var fakeMovies = GetMovieFaker().Generate(4);

            _mockMovieRepository.Setup(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovies);

            var result = await _movieService.SearchMoviesAsync(title, year, default);

            Assert.That(result, Is.EqualTo(fakeMovies));
            _mockMovieRepository.Verify(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task SearchMoviesAsync_WithTitleAndYear_NoMoviesFound()
        {
            var title = "NonExistentMovie";
            var year = 1900;
            var emptyList = new List<Movie>();

            _mockMovieRepository.Setup(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyList);

            var result = await _movieService.SearchMoviesAsync(title, year, default);

            Assert.That(result, Is.Empty);
            _mockMovieRepository.Verify(repo => repo.SearchMoviesAsync(title, year, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateMovieReviewAsync_StringOverload_ValidInput_AddsReview()
        {
            var imdbId = "tt0111161";
            var movieFaker = GetMovieFaker(imdbId);
            var fakeMovie = movieFaker.Generate();
            fakeMovie.Reviews = new List<Review>();

            var userOpinion = "A masterpiece of cinema!";
            var userRating = 5;

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovie);
            _mockMovieRepository.Setup(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _movieService.CreateMovieReviewAsync(imdbId, userOpinion, userRating, default);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ImdbId, Is.EqualTo(imdbId));
            Assert.That(result.Reviews, Has.Count.EqualTo(1));
            var review = result.Reviews.First();
            Assert.That(review.UserOpinion, Is.EqualTo(userOpinion));
            Assert.That(review.UserRating, Is.EqualTo(userRating));

            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(
                It.Is<Movie>(m => m.ImdbId == imdbId && m.Reviews.Count == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void CreateMovieReviewAsync_StringOverload_MovieNotFound_ThrowsKeyNotFoundException()
        {
            var imdbId = "tt9999999"; // Non-existent IMDb ID
            var userOpinion = "Doesn't matter";
            var userRating = 3;

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Movie)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _movieService.CreateMovieReviewAsync(imdbId, userOpinion, userRating, default));

            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task CreateMovieReviewAsync_DtoOverload_ValidDto_AddsReview()
        {
            var imdbId = "tt0120338";
            var movieFaker = GetMovieFaker(imdbId);
            var fakeMovie = movieFaker.Generate();
            fakeMovie.Reviews = new List<Review>();

            var dto = new CreateMovieReviewDto
            {
                ImdbId = imdbId,
                UserOpinion = "Visually stunning and epic.",
                UserRating = 4
            };

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(imdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(fakeMovie);
            _mockMovieRepository.Setup(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var result = await _movieService.CreateMovieReviewAsync(dto, default);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.ImdbId, Is.EqualTo(dto.ImdbId));
            Assert.That(result.Reviews, Has.Count.EqualTo(1));
            var review = result.Reviews.First();
            Assert.That(review.UserOpinion, Is.EqualTo(dto.UserOpinion));
            Assert.That(review.UserRating, Is.EqualTo(dto.UserRating));

            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(dto.ImdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(
                It.Is<Movie>(m => m.ImdbId == dto.ImdbId && m.Reviews.Count == 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void CreateMovieReviewAsync_DtoOverload_MovieNotFound_ThrowsKeyNotFoundException()
        {
            var dto = new CreateMovieReviewDto
            {
                ImdbId = "tt8888888",
                UserOpinion = "Great attempt",
                UserRating = 2
            };

            _mockMovieRepository.Setup(repo => repo.GetByImdbIdAsync(dto.ImdbId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Movie)null);

            Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _movieService.CreateMovieReviewAsync(dto, default));

            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(dto.ImdbId, It.IsAny<CancellationToken>()), Times.Once);
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public void CreateMovieReviewAsync_DtoOverload_NullDto_ThrowsArgumentNullException()
        {
            CreateMovieReviewDto dto = null;

            Assert.ThrowsAsync<ArgumentNullException>(() =>
                _movieService.CreateMovieReviewAsync(dto, default));

            _mockMovieRepository.Verify(repo => repo.GetByImdbIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockMovieRepository.Verify(repo => repo.AddOrUpdateMovieAsync(It.IsAny<Movie>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
