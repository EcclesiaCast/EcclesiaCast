using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class MediaProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Behavior",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "MediaItems",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "EndBehavior",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "Muted",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Scaling",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Volume",
                table: "MediaItems",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Behavior",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "EndBehavior",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Muted",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Scaling",
                table: "MediaItems");

            migrationBuilder.DropColumn(
                name: "Volume",
                table: "MediaItems");
        }
    }
}
