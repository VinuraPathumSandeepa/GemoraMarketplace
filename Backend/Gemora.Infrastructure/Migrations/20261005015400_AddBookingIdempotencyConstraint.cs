using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingIdempotencyConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add unique constraint on ExternalShipmentReference to prevent duplicate bookings
            // This ensures database-level idempotency even under concurrent requests
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""UX_Shipments_ExternalShipmentReference"" 
                ON ""Shipments"" (""ExternalShipmentReference"") 
                WHERE ""ExternalShipmentReference"" IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""UX_Shipments_ExternalShipmentReference"";");
        }
    }
}
