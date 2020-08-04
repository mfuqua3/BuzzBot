using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class raid_data : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Raids",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    StartTime = table.Column<DateTime>(nullable: false),
                    EndTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Raids", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RaidAlias",
                columns: table => new
                {
                    RaidId = table.Column<Guid>(nullable: false),
                    AliasId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidAlias", x => new { x.RaidId, x.AliasId });
                    table.ForeignKey(
                        name: "FK_RaidAlias_Aliases_AliasId",
                        column: x => x.AliasId,
                        principalTable: "Aliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidAlias_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RaidItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<int>(nullable: false),
                    RaidId = table.Column<Guid>(nullable: false),
                    TransactionId = table.Column<Guid>(nullable: false),
                    AwardedAliasId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RaidItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RaidItems_Aliases_AwardedAliasId",
                        column: x => x.AwardedAliasId,
                        principalTable: "Aliases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidItems_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidItems_Raids_RaidId",
                        column: x => x.RaidId,
                        principalTable: "Raids",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RaidItems_EpgpTransactions_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "EpgpTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RaidAlias_AliasId",
                table: "RaidAlias",
                column: "AliasId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidItems_AwardedAliasId",
                table: "RaidItems",
                column: "AwardedAliasId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidItems_ItemId",
                table: "RaidItems",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidItems_RaidId",
                table: "RaidItems",
                column: "RaidId");

            migrationBuilder.CreateIndex(
                name: "IX_RaidItems_TransactionId",
                table: "RaidItems",
                column: "TransactionId",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RaidAlias");

            migrationBuilder.DropTable(
                name: "RaidItems");

            migrationBuilder.DropTable(
                name: "Raids");
        }
    }
}
