using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ThemeId",
                table: "Songs",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Themes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Kind = table.Column<int>(type: "INTEGER", nullable: false),
                    FontFamily = table.Column<string>(type: "TEXT", nullable: false),
                    MaxFontSize = table.Column<double>(type: "REAL", nullable: false),
                    MinFontSize = table.Column<double>(type: "REAL", nullable: false),
                    Bold = table.Column<bool>(type: "INTEGER", nullable: false),
                    Italic = table.Column<bool>(type: "INTEGER", nullable: false),
                    Uppercase = table.Column<bool>(type: "INTEGER", nullable: false),
                    Shadow = table.Column<bool>(type: "INTEGER", nullable: false),
                    TextColor = table.Column<string>(type: "TEXT", nullable: false),
                    AlignH = table.Column<int>(type: "INTEGER", nullable: false),
                    AlignV = table.Column<int>(type: "INTEGER", nullable: false),
                    MarginHorizontal = table.Column<double>(type: "REAL", nullable: false),
                    MarginVertical = table.Column<double>(type: "REAL", nullable: false),
                    BackgroundColor = table.Column<string>(type: "TEXT", nullable: false),
                    BackgroundImagePath = table.Column<string>(type: "TEXT", nullable: true),
                    BackgroundDim = table.Column<double>(type: "REAL", nullable: false),
                    ShowCaption = table.Column<bool>(type: "INTEGER", nullable: false),
                    CaptionPosition = table.Column<int>(type: "INTEGER", nullable: false),
                    CaptionFontSize = table.Column<double>(type: "REAL", nullable: false),
                    ShowVersionName = table.Column<bool>(type: "INTEGER", nullable: false),
                    ShowVerseNumbers = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Themes", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Themes");

            migrationBuilder.DropColumn(
                name: "ThemeId",
                table: "Songs");
        }
    }
}
