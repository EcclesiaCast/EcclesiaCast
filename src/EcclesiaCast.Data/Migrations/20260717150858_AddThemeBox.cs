using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThemeBox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BoxHeight",
                table: "Themes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BoxWidth",
                table: "Themes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BoxX",
                table: "Themes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "BoxY",
                table: "Themes",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FitToWidth",
                table: "Themes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BoxHeight",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "BoxWidth",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "BoxX",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "BoxY",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "FitToWidth",
                table: "Themes");
        }
    }
}
