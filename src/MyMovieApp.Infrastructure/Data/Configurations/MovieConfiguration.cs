using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyMovieApp.Domain.Entities;

namespace MyMovieApp.Infrastructure.Data.Configurations;

public class MovieConfiguration : IEntityTypeConfiguration<Movie>
{
    public void Configure(EntityTypeBuilder<Movie> builder)
    {
        // Nome da tabela no banco de dados
        builder.ToTable("Movies");

        // Chave Primária
        builder.HasKey(m => m.ImdbId);

        // Configuração das propriedades
        builder.Property(m => m.ImdbId)
            .ValueGeneratedNever(); // A chave é fornecida externamente (não gerada pelo DB)

        builder.Property(m => m.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(m => m.Year)
            .IsRequired();

        builder.Property(m => m.Year);
        builder.Property(m => m.Genre);
        builder.Property(m => m.Director);
        builder.Property(m => m.ImdbRating);
        builder.Property(m => m.Plot);

        builder
            .HasIndex(m => new { m.Title, m.Year })
            .HasDatabaseName("idx_movies_title_year");

        builder.OwnsMany(m => m.Actor, reviewBuilder =>
        {
            reviewBuilder.ToTable("Actors");

            reviewBuilder.WithOwner().HasForeignKey("MovieImdbId");

            reviewBuilder.HasKey(r => r.Id);

            reviewBuilder.Property(r => r.Id)
                .ValueGeneratedOnAdd();

            reviewBuilder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(1000);
        });

        builder.HasMany(m => m.Reviews)
            .WithOne(x => x.Movie)
            .HasForeignKey(x => x.ImdbId);

        var navigationReviews = builder.Metadata.FindNavigation(nameof(Movie.Reviews));
        navigationReviews.SetPropertyAccessMode(PropertyAccessMode.Field);

        var navigationActor = builder.Metadata.FindNavigation(nameof(Movie.Actor));
        navigationActor.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}