using Microsoft.EntityFrameworkCore.Migrations;

namespace BuzzBotData.Migrations
{
    public partial class add_inventoryslot_column_to_items : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventorySlot",
                table: "Item",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventorySlot",
                table: "Item");
        }
    }
}
