using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaFraming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FillColor",
                table: "MediaItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "#000000");

            migrationBuilder.AddColumn<int>(
                name: "FillMediaId",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameHeight",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameWidth",
                table: "MediaItems",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OffsetX",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OffsetY",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Zoom",
                table: "MediaItems",
                type: "REAL",
                nullable: false,
                // 1 = sin zoom: un 0 heredado dejaría los fondos que ya existen
                // reducidos a nada.
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FillColor",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "FillMediaId",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "FrameHeight",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "FrameWidth",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "OffsetX",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "OffsetY",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Zoom",
                table: "MediaItems");
        }
    }
}
