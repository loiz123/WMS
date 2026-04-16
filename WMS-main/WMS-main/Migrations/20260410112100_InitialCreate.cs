using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace WMS.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "APP_USERS",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APP_USERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SUPPLIERS",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Phone = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Address = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SUPPLIERS", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TRANSACTIONS",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Type = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    UserId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TransactionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRANSACTIONS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TRANSACTIONS_APP_USERS_UserId",
                        column: x => x.UserId,
                        principalTable: "APP_USERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PRODUCTS",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    MinQuantity = table.Column<int>(type: "integer", nullable: false),
                    Price = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    SupplierId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRODUCTS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PRODUCTS_SUPPLIERS_SupplierId",
                        column: x => x.SupplierId,
                        principalTable: "SUPPLIERS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TRANSACTION_ITEMS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionId = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    ProductId = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "numeric(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRANSACTION_ITEMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TRANSACTION_ITEMS_PRODUCTS_ProductId",
                        column: x => x.ProductId,
                        principalTable: "PRODUCTS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TRANSACTION_ITEMS_TRANSACTIONS_TransactionId",
                        column: x => x.TransactionId,
                        principalTable: "TRANSACTIONS",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "APP_USERS",
                columns: new[] { "Id", "CreatedAt", "FullName", "PasswordHash", "Role", "Username" },
                values: new object[,]
                {
                    { "U001", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Quản trị viên", "$2a$11$McOyEpUFz2htXEoKkBAR5Obw1wwAaGx5uBUk0guOmPIfyiZ0RTu/O", "Admin", "admin" },
                    { "U002", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "Nhân viên kho", "$2a$11$ht2e0R3fyp625eQWWNo/KOmYJabT747Ihhb075m9dIA50TBbvY8W6", "Staff", "staff" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_APP_USERS_Username",
                table: "APP_USERS",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PRODUCTS_SupplierId",
                table: "PRODUCTS",
                column: "SupplierId");

            migrationBuilder.CreateIndex(
                name: "IX_TRANSACTION_ITEMS_ProductId",
                table: "TRANSACTION_ITEMS",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_TRANSACTION_ITEMS_TransactionId",
                table: "TRANSACTION_ITEMS",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_TRANSACTIONS_UserId",
                table: "TRANSACTIONS",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TRANSACTION_ITEMS");

            migrationBuilder.DropTable(
                name: "PRODUCTS");

            migrationBuilder.DropTable(
                name: "TRANSACTIONS");

            migrationBuilder.DropTable(
                name: "SUPPLIERS");

            migrationBuilder.DropTable(
                name: "APP_USERS");
        }
    }
}
