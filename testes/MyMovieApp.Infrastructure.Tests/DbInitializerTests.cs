using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using MyMovieApp.Infrastructure.Data;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection; // Required for TypeInfo
using System.Collections.Immutable; // Required for ToImmutableDictionary

namespace MyMovieApp.Infrastructure.Tests.Data
{
    [TestFixture]
    public class DbInitializerTests
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IServiceScopeFactory> _serviceScopeFactoryMock;
        private Mock<IServiceScope> _serviceScopeMock;
        private Mock<IHostEnvironment> _hostEnvironmentMock;
        private Mock<IDbContextFactory<MoviesDbContext>> _dbContextFactoryMock;
        private Mock<MoviesDbContext> _moviesDbContextMock;
        private Mock<DatabaseFacade> _databaseFacadeMock;

        // Mocks for internal EF Core services
        private Mock<IServiceProvider> _internalServiceProviderMock;
        private Mock<IMigrator> _migratorMock;
        private Mock<IHistoryRepository> _historyRepositoryMock;
        private Mock<IMigrationsAssembly> _migrationsAssemblyMock;
        private Mock<IRelationalConnection> _relationalConnectionMock;

        [SetUp]
        public void Setup()
        {
            _serviceProviderMock = new Mock<IServiceProvider>();
            _serviceScopeFactoryMock = new Mock<IServiceScopeFactory>();
            _serviceScopeMock = new Mock<IServiceScope>();
            _hostEnvironmentMock = new Mock<IHostEnvironment>();
            _dbContextFactoryMock = new Mock<IDbContextFactory<MoviesDbContext>>();

            var options = new DbContextOptionsBuilder<MoviesDbContext>()
                .UseInMemoryDatabase("TestDbForMocking") // Configure a provider
                .Options;
            _moviesDbContextMock = new Mock<MoviesDbContext>(options) { CallBase = true }; // Enable calling base methods like OnModelCreating

            _databaseFacadeMock = new Mock<DatabaseFacade>(_moviesDbContextMock.Object);
            _moviesDbContextMock.Setup(c => c.Database).Returns(_databaseFacadeMock.Object);

            // Setup for internal service provider
            _internalServiceProviderMock = new Mock<IServiceProvider>();
            _databaseFacadeMock.As<IInfrastructure<IServiceProvider>>().Setup(infra => infra.Instance).Returns(_internalServiceProviderMock.Object);

            // Mock specific internal services
            _migratorMock = new Mock<IMigrator>();
            _internalServiceProviderMock.Setup(sp => sp.GetService(typeof(IMigrator))).Returns(_migratorMock.Object);

            _historyRepositoryMock = new Mock<IHistoryRepository>();
            _internalServiceProviderMock.Setup(sp => sp.GetService(typeof(IHistoryRepository))).Returns(_historyRepositoryMock.Object);
            _historyRepositoryMock.Setup(hr => hr.GetAppliedMigrationsAsync(It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(new List<HistoryRow>()); // Default: No migrations applied

            _migrationsAssemblyMock = new Mock<IMigrationsAssembly>();
            _internalServiceProviderMock.Setup(sp => sp.GetService(typeof(IMigrationsAssembly))).Returns(_migrationsAssemblyMock.Object);
            var mockMigrations = new Dictionary<string, TypeInfo> {
                { "migration1", typeof(MigrationSubclass1).GetTypeInfo() },
                { "migration2", typeof(MigrationSubclass2).GetTypeInfo() }
            };
            _migrationsAssemblyMock.Setup(ma => ma.Migrations).Returns(mockMigrations.ToImmutableDictionary());


            _relationalConnectionMock = new Mock<IRelationalConnection>();
            _internalServiceProviderMock.Setup(sp => sp.GetService(typeof(IRelationalConnection))).Returns(_relationalConnectionMock.Object);
            _relationalConnectionMock.Setup(rc => rc.ConnectionString).Returns("TestConnection");

            // Setup for ModelSnapshot and IModel
            var modelSnapshotMock = new Mock<ModelSnapshot>();
            var modelMock = new Mock<Microsoft.EntityFrameworkCore.Metadata.IModel>();
            modelSnapshotMock.Setup(ms => ms.Model).Returns(modelMock.Object);
            _migrationsAssemblyMock.Setup(ma => ma.ModelSnapshot).Returns(modelSnapshotMock.Object);


            // Service provider setup for DbInitializer itself
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IServiceScopeFactory))).Returns(_serviceScopeFactoryMock.Object);
            _serviceScopeFactoryMock.Setup(ssf => ssf.CreateScope()).Returns(_serviceScopeMock.Object);
            _serviceScopeMock.Setup(ss => ss.ServiceProvider).Returns(_serviceProviderMock.Object);

            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IHostEnvironment))).Returns(_hostEnvironmentMock.Object);
            _serviceProviderMock.Setup(sp => sp.GetService(typeof(IDbContextFactory<MoviesDbContext>))).Returns(_dbContextFactoryMock.Object);

            _dbContextFactoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(_moviesDbContextMock.Object);
        }

        [Test]
        public async Task Initialize_WhenNotProductionAndHasPendingMigrations_ShouldCallMigrateAsync()
        {
            // Arrange
            _hostEnvironmentMock.Setup(env => env.EnvironmentName).Returns(Environments.Development);
            // Default setup: Applied migrations are empty, Assembly migrations are "migration1", "migration2"
            // This should result in GetPendingMigrationsAsync returning "migration1", "migration2"

            // Act
            await DbInitializer.Initialize(_serviceProviderMock.Object);

            // Assert
            _dbContextFactoryMock.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
            _historyRepositoryMock.Verify(hr => hr.GetAppliedMigrationsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _migrationsAssemblyMock.Verify(ma => ma.Migrations, Times.AtLeastOnce()); // GetPendingMigrations uses it
            _migratorMock.Verify(m => m.MigrateAsync(null, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Initialize_WhenNotProductionAndNoPendingMigrations_ShouldNotCallMigrateAsync()
        {
            // Arrange
            _hostEnvironmentMock.Setup(env => env.EnvironmentName).Returns(Environments.Development);

            var appliedMigrations = new List<HistoryRow>
            {
                new HistoryRow("migration1", "EFCore.Test"),
                new HistoryRow("migration2", "EFCore.Test")
            };
            _historyRepositoryMock.Setup(hr => hr.GetAppliedMigrationsAsync(It.IsAny<CancellationToken>()))
                                  .ReturnsAsync(appliedMigrations);

            // Assembly migrations are "migration1", "migration2" (default setup)
            // So, GetPendingMigrationsAsync should return an empty list

            // Act
            await DbInitializer.Initialize(_serviceProviderMock.Object);

            // Assert
            _dbContextFactoryMock.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Once);
            _historyRepositoryMock.Verify(hr => hr.GetAppliedMigrationsAsync(It.IsAny<CancellationToken>()), Times.Once);
            _migrationsAssemblyMock.Verify(ma => ma.Migrations, Times.AtLeastOnce());
            _migratorMock.Verify(m => m.MigrateAsync(null, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Initialize_WhenProduction_ShouldDoNothingRelatedToDbCreationOrMigration()
        {
            // Arrange
            _hostEnvironmentMock.Setup(env => env.EnvironmentName).Returns(Environments.Production);

            // Act
            await DbInitializer.Initialize(_serviceProviderMock.Object);

            // Assert
            _dbContextFactoryMock.Verify(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()), Times.Never);
            _migratorMock.Verify(m => m.MigrateAsync(null, It.IsAny<CancellationToken>()), Times.Never);
            _historyRepositoryMock.Verify(hr => hr.GetAppliedMigrationsAsync(It.IsAny<CancellationToken>()), Times.Never);
            _migrationsAssemblyMock.Verify(ma => ma.Migrations, Times.Never);
        }
    }

    // Dummy Migration subclasses for TypeInfo
    public class MigrationSubclass1 : Migration { protected override void Up(MigrationBuilder migrationBuilder) { } }
    public class MigrationSubclass2 : Migration { protected override void Up(MigrationBuilder migrationBuilder) { } }
}
