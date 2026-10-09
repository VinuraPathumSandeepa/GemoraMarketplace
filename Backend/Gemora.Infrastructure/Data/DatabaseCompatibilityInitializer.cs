using Microsoft.EntityFrameworkCore;

namespace Gemora.Infrastructure.Data;

public static class DatabaseCompatibilityInitializer
{
    public static async Task EnsureComponent2SchemaAsync(
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);


        var commands =
            new List<string>
            {
                // =================================================
                // ORDERS
                // =================================================

                """
                ALTER TABLE "Orders"
                ADD COLUMN IF NOT EXISTS
                "FulfillmentStatus"
                character varying(50)
                NOT NULL
                DEFAULT 'Pending';
                """,

                """
                ALTER TABLE "Orders"
                ADD COLUMN IF NOT EXISTS
                "HandedOverAt"
                timestamp with time zone NULL;
                """,

                """
                ALTER TABLE "Orders"
                ADD COLUMN IF NOT EXISTS
                "DeliveredAt"
                timestamp with time zone NULL;
                """,

                """
                UPDATE "Orders"
                SET "FulfillmentStatus" = 'Pending'
                WHERE "FulfillmentStatus" IS NULL
                   OR BTRIM("FulfillmentStatus") = '';
                """,


                // =================================================
                // ORDER DELIVERY DETAILS
                // =================================================

                """
                CREATE TABLE IF NOT EXISTS
                "OrderDeliveryDetails"
                (
                    "Id" uuid NOT NULL,

                    "OrderId" uuid NOT NULL,

                    "RecipientName"
                        character varying(150)
                        NOT NULL,

                    "RecipientPhone"
                        character varying(30)
                        NOT NULL,

                    "AlternatePhone"
                        character varying(30)
                        NULL,

                    "AddressLine1"
                        character varying(250)
                        NOT NULL,

                    "AddressLine2"
                        character varying(250)
                        NULL,

                    "City"
                        character varying(100)
                        NOT NULL,

                    "District"
                        character varying(100)
                        NOT NULL,

                    "Region"
                        character varying(100)
                        NOT NULL,

                    "PostalCode"
                        character varying(20)
                        NOT NULL,

                    "CountryCode"
                        character varying(2)
                        NOT NULL,

                    "NearestLandmark"
                        character varying(250)
                        NULL,

                    "DeliveryInstructions"
                        character varying(1000)
                        NULL,

                    "SignatureRequired"
                        boolean
                        NOT NULL
                        DEFAULT TRUE,

                    "CreatedAt"
                        timestamp with time zone
                        NOT NULL,

                    "UpdatedAt"
                        timestamp with time zone
                        NULL,

                    "LastUpdatedByUserId"
                        uuid
                        NULL,

                    "LockedAt"
                        timestamp with time zone
                        NULL,

                    CONSTRAINT
                    "PK_OrderDeliveryDetails"
                        PRIMARY KEY ("Id"),

                    CONSTRAINT
                    "FK_OrderDeliveryDetails_Orders_OrderId"
                        FOREIGN KEY ("OrderId")
                        REFERENCES "Orders" ("Id")
                        ON DELETE CASCADE
                );
                """,


                // -------------------------------------------------
                // Upgrade existing OrderDeliveryDetails table
                // -------------------------------------------------

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "AlternatePhone"
                character varying(30) NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "AddressLine2"
                character varying(250) NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "NearestLandmark"
                character varying(250) NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "DeliveryInstructions"
                character varying(1000) NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "SignatureRequired"
                boolean
                NOT NULL
                DEFAULT TRUE;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "UpdatedAt"
                timestamp with time zone NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "LastUpdatedByUserId"
                uuid NULL;
                """,

                """
                ALTER TABLE "OrderDeliveryDetails"
                ADD COLUMN IF NOT EXISTS
                "LockedAt"
                timestamp with time zone NULL;
                """,

                """
                CREATE UNIQUE INDEX IF NOT EXISTS
                "IX_OrderDeliveryDetails_OrderId"
                ON "OrderDeliveryDetails" ("OrderId");
                """,


                // =================================================
                // SHIPMENTS
                // =================================================

                """
                CREATE TABLE IF NOT EXISTS
                "Shipments"
                (
                    "Id"
                        uuid NOT NULL,

                    "OrderId"
                        uuid NOT NULL,

                    "CourierName"
                        character varying(150)
                        NOT NULL,

                    "TrackingNumber"
                        character varying(150)
                        NOT NULL,

                    "TrackingUrl"
                        character varying(1000)
                        NULL,

                    "ExpectedDeliveryDate"
                        timestamp with time zone
                        NULL,

                    "DispatchNote"
                        character varying(1000)
                        NULL,

                    "Status"
                        character varying(50)
                        NOT NULL,

                    "CreatedAt"
                        timestamp with time zone
                        NOT NULL,

                    "UpdatedAt"
                        timestamp with time zone
                        NULL,

                    "HandedOverAt"
                        timestamp with time zone
                        NULL,

                    "DeliveredAt"
                        timestamp with time zone
                        NULL,

                    CONSTRAINT
                    "PK_Shipments"
                        PRIMARY KEY ("Id"),

                    CONSTRAINT
                    "FK_Shipments_Orders_OrderId"
                        FOREIGN KEY ("OrderId")
                        REFERENCES "Orders" ("Id")
                        ON DELETE CASCADE
                );
                """,


                // -------------------------------------------------
                // Upgrade existing Shipments table
                //
                // THIS FIXES YOUR CURRENT DispatchNote ERROR.
                // -------------------------------------------------

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "CourierName"
                character varying(150)
                NOT NULL
                DEFAULT '';
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "TrackingNumber"
                character varying(150)
                NOT NULL
                DEFAULT '';
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "TrackingUrl"
                character varying(1000)
                NULL;
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "ExpectedDeliveryDate"
                timestamp with time zone
                NULL;
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "DispatchNote"
                character varying(1000)
                NULL;
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "Status"
                character varying(50)
                NOT NULL
                DEFAULT 'HandedOverToCourier';
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "CreatedAt"
                timestamp with time zone
                NOT NULL
                DEFAULT NOW();
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "UpdatedAt"
                timestamp with time zone
                NULL;
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "HandedOverAt"
                timestamp with time zone
                NULL;
                """,

                """
                ALTER TABLE "Shipments"
                ADD COLUMN IF NOT EXISTS
                "DeliveredAt"
                timestamp with time zone
                NULL;
                """,

                """
                CREATE UNIQUE INDEX IF NOT EXISTS
                "IX_Shipments_OrderId"
                ON "Shipments" ("OrderId");
                """,

                """
                CREATE UNIQUE INDEX IF NOT EXISTS
                "IX_Shipments_TrackingNumber"
                ON "Shipments" ("TrackingNumber");
                """,


                // =================================================
                // FULFILLMENT HISTORY
                // =================================================

                """
                CREATE TABLE IF NOT EXISTS
                "FulfillmentStatusHistories"
                (
                    "Id"
                        integer
                        GENERATED BY DEFAULT
                        AS IDENTITY,

                    "OrderId"
                        uuid
                        NOT NULL,

                    "PreviousStatus"
                        character varying(50)
                        NULL,

                    "NewStatus"
                        character varying(50)
                        NOT NULL,

                    "ChangedByUserId"
                        uuid
                        NULL,

                    "Note"
                        character varying(1000)
                        NULL,

                    "CreatedAt"
                        timestamp with time zone
                        NOT NULL,

                    CONSTRAINT
                    "PK_FulfillmentStatusHistories"
                        PRIMARY KEY ("Id"),

                    CONSTRAINT
                    "FK_FulfillmentStatusHistories_Orders_OrderId"
                        FOREIGN KEY ("OrderId")
                        REFERENCES "Orders" ("Id")
                        ON DELETE CASCADE,

                    CONSTRAINT
                    "FK_FulfillmentStatusHistories_Users_ChangedByUserId"
                        FOREIGN KEY ("ChangedByUserId")
                        REFERENCES "Users" ("Id")
                        ON DELETE SET NULL
                );
                """,


                // -------------------------------------------------
                // Upgrade existing history table
                // -------------------------------------------------

                """
                ALTER TABLE "FulfillmentStatusHistories"
                ADD COLUMN IF NOT EXISTS
                "PreviousStatus"
                character varying(50)
                NULL;
                """,

                """
                ALTER TABLE "FulfillmentStatusHistories"
                ADD COLUMN IF NOT EXISTS
                "ChangedByUserId"
                uuid
                NULL;
                """,

                """
                ALTER TABLE "FulfillmentStatusHistories"
                ADD COLUMN IF NOT EXISTS
                "Note"
                character varying(1000)
                NULL;
                """,

                """
                CREATE INDEX IF NOT EXISTS
                "IX_FulfillmentStatusHistories_OrderId"
                ON "FulfillmentStatusHistories"
                ("OrderId");
                """,

                """
                CREATE INDEX IF NOT EXISTS
                "IX_FulfillmentStatusHistories_ChangedByUserId"
                ON "FulfillmentStatusHistories"
                ("ChangedByUserId");
                """,

                """
                CREATE INDEX IF NOT EXISTS
                "IX_FulfillmentStatusHistories_CreatedAt"
                ON "FulfillmentStatusHistories"
                ("CreatedAt");
                """,


                // =================================================
                // BUYER WISHLIST
                // =================================================

                """
                CREATE TABLE IF NOT EXISTS
                "WishlistItems"
                (
                    "UserId" uuid NOT NULL,
                    "GemListingId" integer NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),

                    CONSTRAINT "PK_WishlistItems"
                        PRIMARY KEY ("UserId", "GemListingId"),
                    CONSTRAINT "FK_WishlistItems_Users_UserId"
                        FOREIGN KEY ("UserId")
                        REFERENCES "Users" ("Id")
                        ON DELETE CASCADE,
                    CONSTRAINT "FK_WishlistItems_GemListings_GemListingId"
                        FOREIGN KEY ("GemListingId")
                        REFERENCES "GemListings" ("Id")
                        ON DELETE CASCADE
                );
                """,

                // Upgrade a pre-existing wishlist table that predates save timestamps.
                """
                ALTER TABLE "WishlistItems"
                ADD COLUMN IF NOT EXISTS
                "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW();
                """,

                """
                CREATE INDEX IF NOT EXISTS
                "IX_WishlistItems_GemListingId"
                ON "WishlistItems" ("GemListingId");
                """,


                // =================================================
                // DATABASE-BACKED PROFILE PHOTOS
                // =================================================

                """
                CREATE TABLE IF NOT EXISTS
                "ProfileImages"
                (
                    "Id" uuid NOT NULL,
                    "UserId" uuid NOT NULL,
                    "Content" bytea NOT NULL,
                    "ContentType" character varying(32) NOT NULL,
                    "CreatedAt" timestamp with time zone NOT NULL DEFAULT NOW(),

                    CONSTRAINT "PK_ProfileImages"
                        PRIMARY KEY ("Id"),
                    CONSTRAINT "FK_ProfileImages_Users_UserId"
                        FOREIGN KEY ("UserId")
                        REFERENCES "Users" ("Id")
                        ON DELETE CASCADE
                );
                """,

                """
                CREATE INDEX IF NOT EXISTS
                "IX_ProfileImages_UserId"
                ON "ProfileImages" ("UserId");
                """
            };


        foreach (var command in commands)
        {
            await dbContext.Database
                .ExecuteSqlRawAsync(
                    command,
                    cancellationToken);
        }
    }
}
