using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class epgp_transaction_datetime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TransactionDateTime",
                table: "EpgpTransactions",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TransactionDateTime",
                table: "EpgpTransactions");
        }
    }
}
