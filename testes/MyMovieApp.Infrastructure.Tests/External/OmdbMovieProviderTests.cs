using Bogus;
using Microsoft.Extensions.Configuration;
using Moq;
using MyMovieApp.Domain.Entities;
using MyMovieApp.Infrastructure.External;
using MyMovieApp.Infrastructure.External.Models;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyMovieApp.Infrastructure.Tests.External
{
    [TestFixture]
    public class OmdbMovieProviderTests
    {
        private Mock<IOmdbApi> _mockOmdbApi;
        private Mock<IConfiguration> _mockConfiguration;
        private Mock<IConfigurationSection> _mockConfigurationSection;
        private OmdbMovieProvider _omdbMovieProvider;
        private Faker<OmdbResponse> _omdbResponseFaker;

        [SetUp]
        public void Setup()
        {
            _mockOmdbApi = new Mock<IOmdbApi>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfigurationSection = new Mock<IConfigurationSection>();

            // Setup IConfiguration to return the mock section
            _mockConfiguration.Setup(c => c.GetSection("OMDb:ApiKey"))
                              .Returns(_mockConfigurationSection.Object);

            _omdbMovieProvider = new OmdbMovieProvider(_mockOmdbApi.Object, _mockConfiguration.Object);

            _omdbResponseFaker = new Faker<OmdbResponse>()
                .RuleFor(o => o.Response, "True") // Default to a successful response
                .RuleFor(o => o.ImdbID, f => "tt" + f.Random.ReplaceNumbers("#######"))
                .RuleFor(o => o.Title, f => f.Lorem.Sentence(3).TrimEnd('.'))
                .RuleFor(o => o.Year, f => f.Date.Past(30, DateTime.Now.AddYears(-1)).Year.ToString())
                .RuleFor(o => o.Genre, f => string.Join(", ", f.Lorem.Words(f.Random.Int(1, 3))))
                .RuleFor(o => o.Director, f => f.Name.FullName())
                .RuleFor(o => o.Actors, f => string.Join(", ", Enumerable.Range(0, f.Random.Int(1, 4)).Select(_ => f.Name.FullName())))
                .RuleFor(o => o.Plot, f => f.Lorem.Paragraph())
                .RuleFor(o => o.ImdbRating, f => f.Random.Decimal(1, 10).ToString("0.0"))
                .RuleFor(o => o.Error, f => null as string); // Default to no error
        }

        private void SetupValidApiKey()
        {
            _mockConfigurationSection.Setup(s => s.Value).Returns("test_api_key");
        }

        private void SetupMissingApiKey()
        {
            _mockConfigurationSection.Setup(s => s.Value).Returns((string)null);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_WithValidImdbIdAndApiKey_ShouldReturnMappedMovie()
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Generate();
            string imdbId = fakeResponse.ImdbID;

            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync("test_api_key", imdbId))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync(imdbId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fakeResponse.ImdbID, result.ImdbId);
            Assert.AreEqual(fakeResponse.Title, result.Title);
            Assert.AreEqual(int.Parse(fakeResponse.Year), result.Year);
            Assert.AreEqual(fakeResponse.Plot, result.Plot); // Changed PlotSummary to Plot
            Assert.AreEqual(fakeResponse.ImdbRating, result.ImdbRating); // Changed result.Rating to result.ImdbRating (string comparison)

            var expectedGenres = fakeResponse.Genre.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList();
            CollectionAssert.AreEquivalent(expectedGenres, result.Genre.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries).ToList());

            var expectedActors = fakeResponse.Actors.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(name => Actor.Create(name)).ToList(); // Changed Actor creation
            Assert.AreEqual(expectedActors.Count, result.Actor.Count); // Changed result.Actors to result.Actor
            for(int i = 0; i < expectedActors.Count; i++)
            {
                Assert.AreEqual(expectedActors[i].Name, result.Actor.ToList()[i].Name); // Changed result.Actors to result.Actor
            }
            // Director is mapped to result.Director
            Assert.AreEqual(fakeResponse.Director, result.Director);
            // For now, we ensure the API call was made.
            _mockOmdbApi.Verify(api => api.GetMovieByImdbIdAsync("test_api_key", imdbId), Times.Once);
        }

        [Test]
        public void GetMovieByImdbIdAsync_MissingApiKey_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupMissingApiKey();
            string imdbId = "tt1234567";

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _omdbMovieProvider.GetMovieByImdbIdAsync(imdbId));
            Assert.AreEqual("OMDb API key is not configured.", ex.Message);
            _mockOmdbApi.Verify(api => api.GetMovieByImdbIdAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_ApiReturnsError_ShouldReturnNull()
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Clone()
                .RuleFor(o => o.Response, "False")
                .RuleFor(o => o.Error, "Movie not found!")
                .Generate();
            string imdbId = "tt_invalid";

            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync("test_api_key", imdbId))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync(imdbId);

            // Assert
            Assert.IsNull(result);
            _mockOmdbApi.Verify(api => api.GetMovieByImdbIdAsync("test_api_key", imdbId), Times.Once);
        }

        [Test]
        public async Task GetMovieByImdbIdAsync_ApiReturnsNull_ShouldReturnNull()
        {
            // Arrange
            SetupValidApiKey();
            string imdbId = "tt_nonexistent";
            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync("test_api_key", imdbId))
                        .ReturnsAsync(default(OmdbResponse)); // Explicitly default

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync(imdbId);

            // Assert
            Assert.IsNull(result);
        }


        [Test]
        public async Task GetMovieByTitleAsync_WithValidTitleAndApiKey_ShouldReturnMappedMovie()
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Generate();
            string title = fakeResponse.Title;

            _mockOmdbApi.Setup(api => api.GetMovieByTitleAsync("test_api_key", title))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByTitleAsync(title, null); // Added null for year

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(fakeResponse.ImdbID, result.ImdbId);
            Assert.AreEqual(fakeResponse.Title, result.Title);
            // ... (add more assertions similar to GetMovieByImdbIdAsync) ...
            _mockOmdbApi.Verify(api => api.GetMovieByTitleAsync("test_api_key", title), Times.Once);
        }

        [Test]
        public void GetMovieByTitleAsync_MissingApiKey_ShouldThrowInvalidOperationException()
        {
            // Arrange
            SetupMissingApiKey();
            string title = "Some Movie Title";

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _omdbMovieProvider.GetMovieByTitleAsync(title, null)); // Added null for year
            Assert.AreEqual("OMDb API key is not configured.", ex.Message);
        }

        [Test]
        public async Task GetMovieByTitleAsync_ApiReturnsError_ShouldReturnNull()
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Clone()
                .RuleFor(o => o.Response, "False")
                .RuleFor(o => o.Error, "Movie not found!")
                .Generate();
            string title = "NonExistent Title";
            _mockOmdbApi.Setup(api => api.GetMovieByTitleAsync("test_api_key", title))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByTitleAsync(title, null); // Added null for year
            // Removed duplicate line: var result = await _omdbMovieProvider.GetMovieByTitleAsync(title, null);

            // Assert
            Assert.IsNull(result);
        }

        [Test]
        public async Task GetMovieByTitleAsync_ApiReturnsNull_ShouldReturnNull()
        {
            // Arrange
            SetupValidApiKey();
            string title = "Another NonExistent Title";
            // Setup for the overload without year
            _mockOmdbApi.Setup(api => api.GetMovieByTitleAsync("test_api_key", title))
                        .ReturnsAsync(default(OmdbResponse));

            // Act
            var result = await _omdbMovieProvider.GetMovieByTitleAsync(title, null); // Added null for year

            // Assert
            Assert.IsNull(result);
        }

        [TestCase("N/A", (short)0)]
        [TestCase("", (short)0)]
        [TestCase(null, (short)0)] // Using null directly
        [TestCase("1995", (short)1995)]
        [TestCase("1990-1992", (short)1990)] // Range, takes start
        [TestCase("1980-", (short)1980)] // Ongoing, takes start
        public async Task MapMovie_YearParsingVariations(string yearString, short expectedYear) // Changed int to short
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Clone()
                .RuleFor(o => o.Year, yearString)
                .Generate();
            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync("any_id");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedYear, result.Year);
        }

        [TestCase("N/A", "N/A")]
        [TestCase("", "")]
        [TestCase(null, null)] // Using null directly
        [TestCase("7.8", "7.8")]
        public async Task MapMovie_RatingStoredAsStringInEntity(string ratingString, string expectedRatingString)
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Clone()
                .RuleFor(o => o.ImdbRating, ratingString)
                .Generate();
            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync("any_id");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(expectedRatingString, result.ImdbRating); // Assert against string ImdbRating property
        }

        [Test]
        public async Task MapMovie_HandlesNullsAndEmptyStringsForOptionalFieldsGracefully()
        {
            // Arrange
            SetupValidApiKey();
            var fakeResponse = _omdbResponseFaker.Clone()
                .RuleFor(o => o.Genre, f => default(string))
                .RuleFor(o => o.Director, f => default(string))
                .RuleFor(o => o.Actors, f => default(string))
                .RuleFor(o => o.Plot, f => default(string))
                .Generate();
            _mockOmdbApi.Setup(api => api.GetMovieByImdbIdAsync(It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(fakeResponse);

            // Act
            var result = await _omdbMovieProvider.GetMovieByImdbIdAsync("any_id");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(string.Empty, result.Genre);
            Assert.IsTrue(result.Actor.Count == 0); // Changed to result.Actor
            Assert.AreEqual(string.Empty, result.Plot); // Changed to result.Plot
            Assert.AreEqual(string.Empty, result.Director);
        }
    }
}
