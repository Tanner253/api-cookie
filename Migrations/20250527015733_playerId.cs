using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Api.Migrations
{
    /// <inheritdoc />
    public partial class playerId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdMobSsvTransactions_Players_PlayerId",
                table: "AdMobSsvTransactions");

            migrationBuilder.AlterColumn<long>(
                name: "PlayerId",
                table: "AdMobSsvTransactions",
                type: "bigint",
                nullable: true,
                oldClrType: typeof(long),
                oldType: "bigint");

            migrationBuilder.AddForeignKey(
                name: "FK_AdMobSsvTransactions_Players_PlayerId",
                table: "AdMobSsvTransactions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdMobSsvTransactions_Players_PlayerId",
                table: "AdMobSsvTransactions");

            migrationBuilder.AlterColumn<long>(
                name: "PlayerId",
                table: "AdMobSsvTransactions",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AdMobSsvTransactions_Players_PlayerId",
                table: "AdMobSsvTransactions",
                column: "PlayerId",
                principalTable: "Players",
                principalColumn: "PlayerId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
