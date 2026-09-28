using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingPlanApprovalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "ShippingPlans",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ApprovedByUserId",
                table: "ShippingPlans",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "ShippingPlans",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ShippingPlans",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "ShippingPlans");

            migrationBuilder.DropColumn(
                name: "ApprovedByUserId",
                table: "ShippingPlans");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "ShippingPlans");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ShippingPlans");
        }
    }
}
