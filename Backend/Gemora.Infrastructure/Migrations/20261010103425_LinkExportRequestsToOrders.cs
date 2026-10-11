using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class LinkExportRequestsToOrders : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "OrderId",
                table: "ExportRequests",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportRequests_OrderId",
                table: "ExportRequests",
                column: "OrderId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ExportRequests_Orders_OrderId",
                table: "ExportRequests",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExportRequests_Orders_OrderId",
                table: "ExportRequests");

            migrationBuilder.DropIndex(
                name: "IX_ExportRequests_OrderId",
                table: "ExportRequests");

            migrationBuilder.DropColumn(
                name: "OrderId",
                table: "ExportRequests");
        }
    }
}
