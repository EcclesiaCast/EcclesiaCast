using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLineBands : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LineBand",
                table: "Themes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "LineBandColor",
                table: "Themes",
                type: "TEXT",
                nullable: false,
                defaultValue: "#000000");

            migrationBuilder.AddColumn<double>(
                name: "LineBandOpacity",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LineBand",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "LineBandColor",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "LineBandOpacity",
                table: "Themes");
        }
    }
}
