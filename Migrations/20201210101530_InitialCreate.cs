using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace fastmusic.Migrations
{
    /// <summary>
    /// Create database schema
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Apply the migration
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllTracks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    Album = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumArtist = table.Column<string>(type: "TEXT", nullable: true),
                    Performer = table.Column<string>(type: "TEXT", nullable: true),
                    TrackNumber = table.Column<uint>(type: "INTEGER", nullable: true),
                    Year = table.Column<uint>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllTracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LastUpdateTime",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UpdateTime = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LastUpdateTime", x => x.Id);
                });
        }

        /// <summary>
        /// Revert the migration
        /// </summary>
        /// <param name="migrationBuilder"></param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllTracks");

            migrationBuilder.DropTable(
                name: "LastUpdateTime");
        }
    }
}
