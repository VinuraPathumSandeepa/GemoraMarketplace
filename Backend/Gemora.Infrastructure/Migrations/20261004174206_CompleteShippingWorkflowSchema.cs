using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompleteShippingWorkflowSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop existing Component 3 tables if they exist (from manual SQL in Program.cs)
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ShipmentTrackingEvents"" CASCADE;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""InsuranceRecords"" CASCADE;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ShippingPlans"" CASCADE;");
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""Shipments"" CASCADE;");
            // Note: Orders table managed by FixOrdersTable migration, do not recreate
            
            migrationBuilder.CreateTable(
                name: "Shipments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    SellerId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuyerId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    OriginRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DestinationAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DestinationRegion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DestinationCountryCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    PackageDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PackageWeight = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    PackageDimensions = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SpecialHandlingNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    PreferredService = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ExportRequired = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CourierName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    ExternalShipmentReference = table.Column<string>(type: "text", nullable: true),
                    SelectedService = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    BookedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ShippedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shipments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Shipments_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Shipments_Users_BuyerId",
                        column: x => x.BuyerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Shipments_Users_SellerId",
                        column: x => x.SellerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "InsuranceRecords",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeclaredValue = table.Column<decimal>(type: "numeric", nullable: false),
                    CoverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CoverageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PolicyNumber = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PolicyReference = table.Column<string>(type: "text", nullable: true),
                    ProviderName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PremiumAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    PolicyStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    PolicyEndDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Location = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    ExternalEventCode = table.Column<string>(type: "text", nullable: true),
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
                    RiskLevel = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RiskReasons = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    RecommendedServiceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    InsuranceRecommended = table.Column<bool>(type: "boolean", nullable: false),
                    RecommendedCoverageAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    HandlingRequirements = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RequiredDocuments = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Warnings = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    GenerationSource = table.Column<string>(type: "text", nullable: false),
                    ExecutionSummary = table.Column<string>(type: "text", nullable: true),
                    IsApproved = table.Column<bool>(type: "boolean", nullable: false),
                    ApprovedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AdminNotes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "IX_InsuranceRecords_ShipmentId",
                table: "InsuranceRecords",
                column: "ShipmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceRecords_Status",
                table: "InsuranceRecords",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_BuyerId",
                table: "Shipments",
                column: "BuyerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_OrderId",
                table: "Shipments",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_SellerId",
                table: "Shipments",
                column: "SellerId");

            migrationBuilder.CreateIndex(
                name: "IX_Shipments_Status",
                table: "Shipments",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEvents_EventType",
                table: "ShipmentTrackingEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEvents_OccurredAt",
                table: "ShipmentTrackingEvents",
                column: "OccurredAt");

            migrationBuilder.CreateIndex(
                name: "IX_ShipmentTrackingEvents_ShipmentId",
                table: "ShipmentTrackingEvents",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingPlans_IsApproved",
                table: "ShippingPlans",
                column: "IsApproved");

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

            // Note: Orders table managed by FixOrdersTable migration, do not drop here
        }
    }
}
