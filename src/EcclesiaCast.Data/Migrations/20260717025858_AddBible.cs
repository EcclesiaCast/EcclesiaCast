using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BibleVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Abbreviation = table.Column<string>(type: "TEXT", nullable: false),
                    Language = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BibleBooks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    VersionId = table.Column<int>(type: "INTEGER", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleBooks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibleBooks_BibleVersions_VersionId",
                        column: x => x.VersionId,
                        principalTable: "BibleVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BibleVerses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BookId = table.Column<int>(type: "INTEGER", nullable: false),
                    Chapter = table.Column<int>(type: "INTEGER", nullable: false),
                    Verse = table.Column<int>(type: "INTEGER", nullable: false),
                    Text = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BibleVerses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BibleVerses_BibleBooks_BookId",
                        column: x => x.BookId,
                        principalTable: "BibleBooks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BibleBooks_VersionId_Number",
                table: "BibleBooks",
                columns: new[] { "VersionId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BibleVerses_BookId_Chapter_Verse",
                table: "BibleVerses",
                columns: new[] { "BookId", "Chapter", "Verse" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BibleVerses");

            migrationBuilder.DropTable(
                name: "BibleBooks");

            migrationBuilder.DropTable(
                name: "BibleVersions");
        }
    }
}
