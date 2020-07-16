using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Guild",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<string>(nullable: true),
                    Name = table.Column<string>(nullable: true),
                    InviteUrl = table.Column<string>(nullable: true),
                    PublicLinkEnabled = table.Column<bool>(nullable: false, defaultValue: false),
                    PublicUrl = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Guild", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Item",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(nullable: true),
                    Icon = table.Column<string>(nullable: true),
                    Quality = table.Column<string>(nullable: true),
                    Class = table.Column<int>(nullable: false),
                    Subclass = table.Column<int>(nullable: true),
                    RuName = table.Column<string>(nullable: true),
                    DeName = table.Column<string>(nullable: true),
                    FrName = table.Column<string>(nullable: true),
                    CnName = table.Column<string>(nullable: true),
                    ItName = table.Column<string>(nullable: true),
                    EsName = table.Column<string>(nullable: true),
                    PtName = table.Column<string>(nullable: true),
                    KoName = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Item", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Character",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    LastUpdated = table.Column<DateTime>(nullable: true),
                    Gold = table.Column<int>(nullable: false),
                    GuildId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Character", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Character_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GuildRole",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    GuildId = table.Column<Guid>(nullable: false),
                    DisplayName = table.Column<string>(nullable: true),
                    CanUpload = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GuildRole", x => new { x.GuildId, x.UserId });
                    table.ForeignKey(
                        name: "FK_GuildRole_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ItemRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    CharacterName = table.Column<string>(nullable: true),
                    Gold = table.Column<int>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Reason = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: true),
                    GuildId = table.Column<Guid>(nullable: false),
                    DateRequested = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRequest", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRequest_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transaction",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    GuildId = table.Column<Guid>(nullable: false),
                    Type = table.Column<string>(nullable: true),
                    CharacterName = table.Column<string>(nullable: true),
                    Money = table.Column<int>(nullable: true),
                    TransactionDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transaction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transaction_Guild_GuildId",
                        column: x => x.GuildId,
                        principalTable: "Guild",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Bag",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<int>(nullable: true),
                    CharacterId = table.Column<Guid>(nullable: false),
                    isBank = table.Column<bool>(nullable: false),
                    BagContainerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bag", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bag_Character_CharacterId",
                        column: x => x.CharacterId,
                        principalTable: "Character",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Bag_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ItemRequestDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<int>(nullable: true),
                    Quantity = table.Column<int>(nullable: false),
                    ItemRequestId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItemRequestDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItemRequestDetail_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItemRequestDetail_ItemRequest_ItemRequestId",
                        column: x => x.ItemRequestId,
                        principalTable: "ItemRequest",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TransactionDetail",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ItemId = table.Column<int>(nullable: true),
                    Quantity = table.Column<int>(nullable: false),
                    TransactionId = table.Column<Guid>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionDetail", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TransactionDetail_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TransactionDetail_Transaction_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "Transaction",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BagSlot",
                columns: table => new
                {
                    SlotId = table.Column<int>(nullable: false),
                    BagId = table.Column<Guid>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    ItemId = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BagSlot", x => new { x.BagId, x.SlotId });
                    table.ForeignKey(
                        name: "FK_BagSlot_Bag_BagId",
                        column: x => x.BagId,
                        principalTable: "Bag",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BagSlot_Item_ItemId",
                        column: x => x.ItemId,
                        principalTable: "Item",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bag_CharacterId",
                table: "Bag",
                column: "CharacterId");

            migrationBuilder.CreateIndex(
                name: "IX_Bag_ItemId",
                table: "Bag",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BagSlot_ItemId",
                table: "BagSlot",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_Character_GuildId",
                table: "Character",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequest_GuildId",
                table: "ItemRequest",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequestDetail_ItemId",
                table: "ItemRequestDetail",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ItemRequestDetail_ItemRequestId",
                table: "ItemRequestDetail",
                column: "ItemRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_Transaction_GuildId",
                table: "Transaction",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetail_ItemId",
                table: "TransactionDetail",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TransactionDetail_TransactionId",
                table: "TransactionDetail",
                column: "TransactionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BagSlot");

            migrationBuilder.DropTable(
                name: "GuildRole");

            migrationBuilder.DropTable(
                name: "ItemRequestDetail");

            migrationBuilder.DropTable(
                name: "TransactionDetail");

            migrationBuilder.DropTable(
                name: "Bag");

            migrationBuilder.DropTable(
                name: "ItemRequest");

            migrationBuilder.DropTable(
                name: "Transaction");

            migrationBuilder.DropTable(
                name: "Character");

            migrationBuilder.DropTable(
                name: "Item");

            migrationBuilder.DropTable(
                name: "Guild");
        }
    }
}
