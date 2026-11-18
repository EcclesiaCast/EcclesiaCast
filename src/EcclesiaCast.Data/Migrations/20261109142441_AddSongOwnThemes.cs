using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSongOwnThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerSongId",
                table: "Themes",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OwnerSongId",
                table: "Themes");
        }
    }
}
