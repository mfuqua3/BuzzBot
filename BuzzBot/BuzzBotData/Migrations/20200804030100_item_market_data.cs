using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class item_market_data : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Factions",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    ServerId = table.Column<string>(nullable: true),
                    ServerId1 = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Factions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Factions_Servers_ServerId",
                        column: x => x.ServerId,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Factions_Servers_ServerId1",
                        column: x => x.ServerId1,
                        principalTable: "Servers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LiveItemData",
                columns: table => new
                {
                    FactionId = table.Column<string>(nullable: false),
                    ItemId = table.Column<int>(nullable: false),
                    ItemId1 = table.Column<int>(nullable: true),
                    MarketValue = table.Column<int>(nullable: false),
                    HistoricalValue = table.Column<int>(nullable: false),
                    MinimumBuyout = table.Column<int>(nullable: false),
                    NumberOfAuctions = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiveItemData", x => new { x.FactionId, x.ItemId });
                    table.ForeignKey(
                        name: "FK_LiveItemData_Factions_FactionId",
                        column: x => x.FactionId,
                        principalTable: "Factions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveItemData_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LiveItemData_Item_ItemId1",
                        column: x => x.ItemId1,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Factions_ServerId",
                table: "Factions",
                column: "ServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Factions_ServerId1",
                table: "Factions",
                column: "ServerId1");

            migrationBuilder.CreateIndex(
                name: "IX_LiveItemData_ItemId",
                table: "LiveItemData",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_LiveItemData_ItemId1",
                table: "LiveItemData",
                column: "ItemId1");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LiveItemData");

            migrationBuilder.DropTable(
                name: "Factions");

            migrationBuilder.DropTable(
                name: "Servers");
        }
    }
}
