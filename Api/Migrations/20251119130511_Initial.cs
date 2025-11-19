using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public1");

            migrationBuilder.CreateTable(
                name: "GameStats",
                schema: "public1",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalWins = table.Column<int>(type: "integer", nullable: false),
                    TotalGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    TicTacToeWins = table.Column<int>(type: "integer", nullable: false),
                    TicTacToeGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    RockPaperScissorsWins = table.Column<int>(type: "integer", nullable: false),
                    RockPaperScissorsGamesPlayed = table.Column<int>(type: "integer", nullable: false),
                    ConnectFourWins = table.Column<int>(type: "integer", nullable: false),
                    ConnectFourGamesPlayed = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GameStats", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "public1",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Wins = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GameStats",
                schema: "public1");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "public1");
        }
    }
}
