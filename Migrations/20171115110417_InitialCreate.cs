using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace fastmusic.Migrations
{
    /// <summary>
    /// Automatically generated.
    /// Migration for initialising database tables.
    /// </summary>
    public partial class InitialCreate : Migration
    {
        /// <summary>
        /// Automatically generated.
        /// Creates empty database tables.
        /// </summary>
        /// <param name="migrationBuilder">Handle to the database.</param>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AllTracks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Album = table.Column<string>(type: "TEXT", nullable: true),
                    AlbumArtist = table.Column<string>(type: "TEXT", nullable: true),
                    FileName = table.Column<string>(type: "TEXT", nullable: true),
                    Performer = table.Column<string>(type: "TEXT", nullable: true),
                    Title = table.Column<string>(type: "TEXT", nullable: true),
                    TrackNumber = table.Column<uint>(type: "INTEGER", nullable: false),
                    Year = table.Column<uint>(type: "INTEGER", nullable: false)
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
        /// Automatically generated.
        /// Destroys tables created by @see Up
        /// </summary>
        /// <param name="migrationBuilder">Handle to the database.</param>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllTracks");

            migrationBuilder.DropTable(
                name: "LastUpdateTime");
        }
    }
}
