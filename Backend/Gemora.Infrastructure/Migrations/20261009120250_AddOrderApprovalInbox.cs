using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderApprovalInbox : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "PaymentDueAt" timestamp with time zone NULL;
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "BuyerMessageAt" timestamp with time zone NULL;
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "BuyerReadAt" timestamp with time zone NULL;
        ALTER TABLE "Orders" ADD COLUMN IF NOT EXISTS "SellerReadAt" timestamp with time zone NULL;

        -- Preserve contradictory legacy orders without silently cancelling purchases.
        -- Run classification once; future conflicts must fail rather than become exempt.
        DO $upgrade$
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM pg_attribute
                WHERE attrelid = '"Orders"'::regclass AND attname = 'IsLegacyDuplicate' AND NOT attisdropped) THEN
                ALTER TABLE "Orders" ADD COLUMN "IsLegacyDuplicate" boolean NOT NULL DEFAULT false;
                WITH ranked AS (
                    SELECT "Id", "Status",
                        ROW_NUMBER() OVER (PARTITION BY "GemListingId"
                            ORDER BY CASE WHEN "Status" = 'Pending' THEN 2
                                WHEN "PaidAt" IS NOT NULL OR "Status" IN ('Paid', 'Completed', 'Delivered', 'InTransit', 'ShipmentCreated', 'PreparingForShipment') THEN 0
                                ELSE 1 END, "CreatedAt", "Id") AS gem_rank,
                        ROW_NUMBER() OVER (PARTITION BY "GemListingId", "BuyerId"
                            ORDER BY CASE WHEN "Status" = 'Pending' THEN 2
                                WHEN "PaidAt" IS NOT NULL OR "Status" IN ('Paid', 'Completed', 'Delivered', 'InTransit', 'ShipmentCreated', 'PreparingForShipment') THEN 0
                                ELSE 1 END, "CreatedAt", "Id") AS buyer_rank
                    FROM "Orders"
                    WHERE "GemListingId" IS NOT NULL AND "Status" NOT IN ('Rejected', 'Cancelled', 'Refunded', 'Failed')
                )
                UPDATE "Orders" SET "IsLegacyDuplicate" = true
                FROM ranked WHERE "Orders"."Id" = ranked."Id"
                    AND (ranked.buyer_rank > 1 OR (ranked."Status" <> 'Pending' AND ranked.gem_rank > 1));
                DROP INDEX IF EXISTS "IX_Orders_ReservedGem";
                DROP INDEX IF EXISTS "IX_Orders_ActiveBuyerGem";
            END IF;
        END $upgrade$;

        -- Existing approved orders receive one new payment window when the feature is installed.
        UPDATE "Orders" SET "PaymentDueAt" = CURRENT_TIMESTAMP + INTERVAL '3 hours',
            "BuyerMessageAt" = CURRENT_TIMESTAMP
        WHERE "Status" IN ('Confirmed', 'AwaitingPayment') AND "PaymentDueAt" IS NULL AND "PaidAt" IS NULL;

        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_ReservedGem" ON "Orders" ("GemListingId")
        WHERE NOT "IsLegacyDuplicate" AND "Status" NOT IN ('Pending', 'Rejected', 'Cancelled', 'Refunded', 'Failed');
        CREATE UNIQUE INDEX IF NOT EXISTS "IX_Orders_ActiveBuyerGem" ON "Orders" ("GemListingId", "BuyerId")
        WHERE NOT "IsLegacyDuplicate" AND "Status" NOT IN ('Rejected', 'Cancelled', 'Refunded', 'Failed');
        CREATE INDEX IF NOT EXISTS "IX_Orders_Status_PaymentDueAt" ON "Orders" ("Status", "PaymentDueAt");
        """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Orders_ActiveBuyerGem",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_ReservedGem",
                table: "Orders");

            migrationBuilder.DropIndex(
                name: "IX_Orders_Status_PaymentDueAt",
                table: "Orders");

            migrationBuilder.DropColumn(name: "IsLegacyDuplicate", table: "Orders");

            migrationBuilder.DropColumn(
                name: "BuyerMessageAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "BuyerReadAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "PaymentDueAt",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "SellerReadAt",
                table: "Orders");
        }
    }
}
