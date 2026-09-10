using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSlideTransition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Transition",
                table: "Themes",
                type: "INTEGER",
                nullable: false,
                // 1 = Fade: los temas que ya existen siguen con el fundido que
                // venían haciendo, en vez de pasar a un corte seco.
                defaultValue: 1);

            migrationBuilder.AddColumn<double>(
                name: "TransitionMs",
                table: "Themes",
                type: "REAL",
                nullable: false,
                defaultValue: 280.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Transition",
                table: "Themes");

            migrationBuilder.DropColumn(
                name: "TransitionMs",
                table: "Themes");
        }
    }
}
