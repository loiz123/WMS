using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace WMS.API.Migrations
{
    /// <inheritdoc />
    public partial class AddStockTake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "STOCK_TAKES",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Note = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK_TAKES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STOCK_TAKES_APP_USERS_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "APP_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STOCK_TAKES_APP_USERS_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "APP_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STOCK_TAKE_ITEMS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StockTakeId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SystemQuantity = table.Column<int>(type: "integer", nullable: false),
                    ActualQuantity = table.Column<int>(type: "integer", nullable: false),
                    Note = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK_TAKE_ITEMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STOCK_TAKE_ITEMS_PRODUCTS_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STOCK_TAKE_ITEMS_STOCK_TAKES_StockTakeId",
                        column: x => x.StockTakeId,
                        principalTable: "STOCK_TAKES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "APP_USERS",
                keyColumn: "Id",
                keyValue: "U001",
                column: "PasswordHash",
                value: "$2a$11$f0n5ojYky.q6BXjkd0Vz5uAIyNoAe0GsQLq4hKC1YbSdQPWGZFPuG");

            migrationBuilder.UpdateData(
                table: "APP_USERS",
                keyColumn: "Id",
                keyValue: "U002",
                column: "PasswordHash",
                value: "$2a$11$lIShkllABbf0AA88Ns0oH.p48eDAscE6fCshd07iM7gC8PZemIHZu");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_TAKE_ITEMS_ProductId",
                table: "STOCK_TAKE_ITEMS",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_TAKE_ITEMS_StockTakeId",
                table: "STOCK_TAKE_ITEMS",
                column: "StockTakeId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_TAKES_ApprovedBy",
                table: "STOCK_TAKES",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_TAKES_CreatedBy",
                table: "STOCK_TAKES",
                column: "CreatedBy");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "STOCK_TAKE_ITEMS");

            migrationBuilder.DropTable(
                name: "STOCK_TAKES");

            migrationBuilder.UpdateData(
                table: "APP_USERS",
                keyColumn: "Id",
                keyValue: "U001",
                column: "PasswordHash",
                value: "$2a$11$McOyEpUFz2htXEoKkBAR5Obw1wwAaGx5uBUk0guOmPIfyiZ0RTu/O");

            migrationBuilder.UpdateData(
                table: "APP_USERS",
                keyColumn: "Id",
                keyValue: "U002",
                column: "PasswordHash",
                value: "$2a$11$ht2e0R3fyp625eQWWNo/KOmYJabT747Ihhb075m9dIA50TBbvY8W6");
        }
    }
}
