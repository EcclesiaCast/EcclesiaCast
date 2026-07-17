using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCaptionAndSecondaryStyle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CaptionColor",
                table: "Themes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "CaptionFontFamily",
                table: "Themes",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SecondaryColor",
                table: "Themes",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "SecondaryItalic",
                table: "Themes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "SecondaryMatchesPrimary",
                table: "Themes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "SecondaryScale",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CaptionColor",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "CaptionFontFamily",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "SecondaryColor",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "SecondaryItalic",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "SecondaryMatchesPrimary",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "SecondaryScale",
                table: "Themes");
        }
    }
}
