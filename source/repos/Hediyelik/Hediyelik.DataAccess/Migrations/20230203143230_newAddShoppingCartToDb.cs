using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Eticaret.Migrations
{
    /// <inheritdoc />
    public partial class newAddShoppingCartToDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_AspNetUsers_ApplicationUserId",
                table: "ShoppingCarts");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCarts_Products_ProductId",
                table: "ShoppingCarts");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts");

            migrationBuilder.RenameTable(
                name: "ShoppingCarts",
                newName: "ShoppingCartsNew");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCarts_ProductId",
                table: "ShoppingCartsNew",
                newName: "IX_ShoppingCartsNew_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCarts_ApplicationUserId",
                table: "ShoppingCartsNew",
                newName: "IX_ShoppingCartsNew_ApplicationUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingCartsNew",
                table: "ShoppingCartsNew",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCartsNew_AspNetUsers_ApplicationUserId",
                table: "ShoppingCartsNew",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCartsNew_Products_ProductId",
                table: "ShoppingCartsNew",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCartsNew_AspNetUsers_ApplicationUserId",
                table: "ShoppingCartsNew");

            migrationBuilder.DropForeignKey(
                name: "FK_ShoppingCartsNew_Products_ProductId",
                table: "ShoppingCartsNew");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ShoppingCartsNew",
                table: "ShoppingCartsNew");

            migrationBuilder.RenameTable(
                name: "ShoppingCartsNew",
                newName: "ShoppingCarts");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCartsNew_ProductId",
                table: "ShoppingCarts",
                newName: "IX_ShoppingCarts_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_ShoppingCartsNew_ApplicationUserId",
                table: "ShoppingCarts",
                newName: "IX_ShoppingCarts_ApplicationUserId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ShoppingCarts",
                table: "ShoppingCarts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_AspNetUsers_ApplicationUserId",
                table: "ShoppingCarts",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ShoppingCarts_Products_ProductId",
                table: "ShoppingCarts",
                column: "ProductId",
                principalTable: "Products",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
