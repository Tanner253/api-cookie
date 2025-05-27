using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class Admob : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdMobSsvTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    RewardItem = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RewardAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdCompletionTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdMobSsvTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_AdMobSsvTransactions_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdMobSsvTransactions_PlayerId",
                table: "AdMobSsvTransactions",
                column: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdMobSsvTransactions");
        }
    }
}
