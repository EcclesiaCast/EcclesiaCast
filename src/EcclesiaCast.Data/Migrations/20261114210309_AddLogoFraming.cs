using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoFraming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FillMediaId",
                table: "Logos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameHeight",
                table: "Logos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FrameWidth",
                table: "Logos",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "OffsetX",
                table: "Logos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OffsetY",
                table: "Logos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Zoom",
                table: "Logos",
                type: "REAL",
                nullable: false,
                defaultValue: 1.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FillMediaId",
                table: "Logos");

            migrationBuilder.DropColumn(
                name: "FrameHeight",
                table: "Logos");

            migrationBuilder.DropColumn(
                name: "FrameWidth",
                table: "Logos");

            migrationBuilder.DropColumn(
                name: "OffsetX",
                table: "Logos");

            migrationBuilder.DropColumn(
                name: "OffsetY",
                table: "Logos");

            migrationBuilder.DropColumn(
                name: "Zoom",
                table: "Logos");
        }
    }
}
