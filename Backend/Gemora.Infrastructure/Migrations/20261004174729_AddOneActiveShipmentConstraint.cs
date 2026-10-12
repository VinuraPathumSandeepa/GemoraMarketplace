using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gemora.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOneActiveShipmentConstraint : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Enforce one ACTIVE shipment per Order at database level
            // Active statuses: anything NOT in (Delivered, Cancelled, DeliveryFailed)
            // Using PostgreSQL partial unique index
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ""UX_Shipments_OrderId_Active"" 
                ON ""Shipments"" (""OrderId"") 
                WHERE ""Status"" NOT IN ('Delivered', 'Cancelled', 'DeliveryFailed');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP INDEX IF EXISTS ""UX_Shipments_OrderId_Active"";");
        }
    }
}
