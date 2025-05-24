using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class minting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlayerMemeMintDatas",
                columns: table => new
                {
                    PlayerId = table.Column<long>(type: "bigint", nullable: false),
                    PlayerGCMPMPoints = table.Column<decimal>(type: "numeric(28,8)", nullable: false),
                    SharedMintProgress = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerMemeMintDatas", x => x.PlayerId);
                    table.ForeignKey(
                        name: "FK_PlayerMemeMintDatas_Players_PlayerId",
                        column: x => x.PlayerId,
                        principalTable: "Players",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MinterInstances",
                columns: table => new
                {
                    MinterInstanceEntityId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PlayerMemeMintPlayerDataId = table.Column<long>(type: "bigint", nullable: false),
                    ClientInstanceId = table.Column<int>(type: "integer", nullable: false),
                    State = table.Column<int>(type: "integer", nullable: false),
                    TimeRemainingSeconds = table.Column<float>(type: "real", nullable: false),
                    IsUnlocked = table.Column<bool>(type: "boolean", nullable: false),
                    LastCycleStartTimeUTC = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinterInstances", x => x.MinterInstanceEntityId);
                    table.ForeignKey(
                        name: "FK_MinterInstances_PlayerMemeMintDatas_PlayerMemeMintPlayerDat~",
                        column: x => x.PlayerMemeMintPlayerDataId,
                        principalTable: "PlayerMemeMintDatas",
                        principalColumn: "PlayerId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MinterInstances_PlayerMemeMintPlayerDataId_ClientInstanceId",
                table: "MinterInstances",
                columns: new[] { "PlayerMemeMintPlayerDataId", "ClientInstanceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MinterInstances");

            migrationBuilder.DropTable(
                name: "PlayerMemeMintDatas");
        }
    }
}
