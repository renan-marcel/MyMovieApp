using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyMovieApp.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CreateIndexOfMovies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "idx_movies_title_year",
                table: "Movies",
                columns: new[] { "Title", "Year" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_movies_title_year",
                table: "Movies");
        }
    }
}
