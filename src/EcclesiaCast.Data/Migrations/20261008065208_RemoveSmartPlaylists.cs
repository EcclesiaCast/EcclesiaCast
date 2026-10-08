using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EcclesiaCast.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSmartPlaylists : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The lists that filled themselves are gone for good: they kept no
            // items, only a rule, so there is nothing else to save from them.
            migrationBuilder.Sql("DELETE FROM PlaylistItems WHERE PlaylistId IN (SELECT Id FROM Playlists WHERE Rule <> 0);");
            migrationBuilder.Sql("DELETE FROM Playlists WHERE Rule <> 0;");

            migrationBuilder.DropColumn(
                name: "Rule",
                table: "Playlists");

            migrationBuilder.DropColumn(
                name: "RuleValue",
                table: "Playlists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Rule",
                table: "Playlists",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "RuleValue",
                table: "Playlists",
                type: "TEXT",
                nullable: true);
        }
    }
}
