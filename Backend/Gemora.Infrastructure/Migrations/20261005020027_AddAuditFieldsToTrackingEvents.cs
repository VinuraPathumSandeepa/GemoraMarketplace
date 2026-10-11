using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditFieldsToTrackingEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "ExternalEventCode",
                table: "ShipmentTrackingEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NewState",
                table: "ShipmentTrackingEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PerformedByRole",
                table: "ShipmentTrackingEvents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PerformedByUserId",
                table: "ShipmentTrackingEvents",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousState",
                table: "ShipmentTrackingEvents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ShipmentTrackingEvents",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NewState",
                table: "ShipmentTrackingEvents");

            migrationBuilder.DropColumn(
                name: "PerformedByRole",
                table: "ShipmentTrackingEvents");

            migrationBuilder.DropColumn(
                name: "PerformedByUserId",
                table: "ShipmentTrackingEvents");

            migrationBuilder.DropColumn(
                name: "PreviousState",
                table: "ShipmentTrackingEvents");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ShipmentTrackingEvents");

            migrationBuilder.AlterColumn<string>(
                name: "ExternalEventCode",
                table: "ShipmentTrackingEvents",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
