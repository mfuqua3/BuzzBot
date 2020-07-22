using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class add_epgp_datamodel : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GuildUsers",
                columns: table => new
                {
                    Id = table.Column<ulong>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Aliases",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Class = table.Column<int>(nullable: false),
                    EffortPoints = table.Column<int>(nullable: false),
                    GearPoints = table.Column<int>(nullable: false),
                    UserId = table.Column<ulong>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aliases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Aliases_GuildUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "GuildUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EpgpTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    AliasId = table.Column<Guid>(nullable: false),
                    TransactionType = table.Column<int>(nullable: false),
                    Value = table.Column<int>(nullable: false),
                    Memo = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EpgpTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EpgpTransactions_Aliases_AliasId",
                        column: x => x.AliasId,
                        principalTable: "Aliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Aliases_UserId",
                table: "Aliases",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EpgpTransactions_AliasId",
                table: "EpgpTransactions",
                column: "AliasId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EpgpTransactions");

            migrationBuilder.DropTable(
                name: "Aliases");

            migrationBuilder.DropTable(
                name: "GuildUsers");
        }
    }
}
