using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTextOutlineAndCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Uppercase",
                table: "Themes",
                newName: "TextCase");

            migrationBuilder.AddColumn<string>(
                name: "OutlineColor",
                table: "Themes",
                type: "TEXT",
                nullable: false,
                defaultValue: "#000000");

            migrationBuilder.AddColumn<double>(
                name: "OutlineWidth",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadowBlur",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 18.0);

            migrationBuilder.AddColumn<double>(
                name: "ShadowOpacity",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 0.75);

            // Themes saved before this migration have no shadow settings of
            // their own; give them the look they had (blur 18, 75 % opacity)
            // so nothing changes on screen just because the app updated.
            migrationBuilder.Sql(
                "UPDATE Themes SET ShadowBlur = 18, ShadowOpacity = 0.75, OutlineColor = '#000000'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OutlineColor",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "OutlineWidth",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "ShadowBlur",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "ShadowOpacity",
                table: "Themes");

            migrationBuilder.RenameColumn(
                name: "TextCase",
                table: "Themes",
                newName: "Uppercase");
        }
    }
}
