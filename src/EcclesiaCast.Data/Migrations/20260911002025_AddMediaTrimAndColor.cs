using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaTrimAndColor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Brightness",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "Tint",
                table: "MediaItems",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "TintStrength",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrimEnd",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "TrimStart",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Brightness",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Tint",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TintStrength",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TrimEnd",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "TrimStart",
                table: "MediaItems");
        }
    }
}
