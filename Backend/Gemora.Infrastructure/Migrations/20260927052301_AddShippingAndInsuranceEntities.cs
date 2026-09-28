using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddShippingAndInsuranceEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    BuyerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Origin = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Destination = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PackageDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    SelectedService = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CourierName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExternalShipmentReference = table.Column<string>(type: "text", nullable: true),
                    TrackingNumber = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    PolicyReference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CoverageAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CoverageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PremiumAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsuranceRecords_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShipmentTrackingEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LocationText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ExternalEventCode = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShipmentTrackingEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShipmentTrackingEvents_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ShippingPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    RecommendedServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InsuranceRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedCoverage = table.Column<decimal>(type: "numeric", nullable: false),
                    Requirements = table.Column<string>(type: "jsonb", nullable: false),
                    Warnings = table.Column<string>(type: "jsonb", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShippingPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShippingPlans_Shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalTable: "Shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceRecords_PolicyReference",
                table: "InsuranceRecords",
                column: "PolicyReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceRecords_ShipmentId",
                table: "InsuranceRecords",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_BuyerUserId",
                table: "Shipments",
                column: "BuyerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_CreatedAt",
                table: "Shipments",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SellerUserId",
                table: "Shipments",
                column: "SellerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_ShipmentNumber",
                table: "Shipments",
                column: "ShipmentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEvents_OccurredAt",
                table: "ShipmentTrackingEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEvents_ShipmentId",
                table: "ShipmentTrackingEvents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingPlans_ShipmentId",
                table: "ShippingPlans",
                column: "ShipmentId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceRecords");

            migrationBuilder.DropTable(
                name: "ShipmentTrackingEvents");

            migrationBuilder.DropTable(
                name: "ShippingPlans");

            migrationBuilder.DropTable(
                name: "Shipments");
        }
    }
}
