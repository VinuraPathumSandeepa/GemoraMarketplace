using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Gemora.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IConfiguration configuration)
    {
        // ==========================================
        // READ DEVELOPMENT PASSWORDS
        // FROM SECURE CONFIGURATION
        // ==========================================

        var adminPassword =
            configuration["SeedUsers:AdminPassword"];

        var gemologistPassword =
            configuration["SeedUsers:GemologistPassword"];

        var exportOfficerPassword =
            configuration["SeedUsers:ExportOfficerPassword"];

        if (string.IsNullOrWhiteSpace(adminPassword) ||
            string.IsNullOrWhiteSpace(gemologistPassword) ||
            string.IsNullOrWhiteSpace(exportOfficerPassword))
        {
            throw new InvalidOperationException(
                "Seed user passwords are not configured."
            );
        }


        // ==========================================
        // ADMIN
        // ==========================================

        var adminEmail = "admin@gemora.com";

        var existingAdmin = await context.Users.FirstOrDefaultAsync(
            u => u.Email == adminEmail);

        if (existingAdmin != null)
        {
            // Update password to ensure it matches current configuration
            existingAdmin.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(adminPassword);

            existingAdmin.FullName = "Gemora Admin";
            existingAdmin.Role = UserRoles.Admin;
        }
        else
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Admin",

                Email = adminEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        adminPassword
                    ),

                Role = UserRoles.Admin,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // GEMOLOGIST
        // ==========================================

        var gemologistEmail =
            "gemologist@gemora.com";

        var existingGemologist = await context.Users.FirstOrDefaultAsync(
            u => u.Email == gemologistEmail);

        if (existingGemologist != null)
        {
            existingGemologist.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(gemologistPassword);

            existingGemologist.FullName = "Gemora Gemologist";
            existingGemologist.Role = UserRoles.Gemologist;
        }
        else
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Gemologist",

                Email = gemologistEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        gemologistPassword
                    ),

                Role = UserRoles.Gemologist,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // EXPORT OFFICER
        // ==========================================

        var exportOfficerEmail =
            "export@gemora.com";

        var existingExportOfficer = await context.Users.FirstOrDefaultAsync(
            u => u.Email == exportOfficerEmail);

        if (existingExportOfficer != null)
        {
            existingExportOfficer.PasswordHash =
                BCrypt.Net.BCrypt.HashPassword(exportOfficerPassword);

            existingExportOfficer.FullName = "Gemora Export Officer";
            existingExportOfficer.Role = UserRoles.ExportOfficer;
        }
        else
        {
            context.Users.Add(new User
            {
                FullName = "Gemora Export Officer",

                Email = exportOfficerEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        exportOfficerPassword
                    ),

                Role = UserRoles.ExportOfficer,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // SELLER (for Component 3 shipping demo)
        // ==========================================

        var sellerEmail = "seller@gemora.com";

        if (!await context.Users.AnyAsync(
                u => u.Email == sellerEmail))
        {
            context.Users.Add(new User
            {
                FullName = "Demo Seller",

                Email = sellerEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "seller123"
                    ),

                Role = UserRoles.Seller,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // BUYER (for Component 3 tracking demo)
        // ==========================================

        var buyerEmail = "buyer@gemora.com";

        if (!await context.Users.AnyAsync(
                u => u.Email == buyerEmail))
        {
            context.Users.Add(new User
            {
                FullName = "Demo Buyer",

                Email = buyerEmail,

                PasswordHash =
                    BCrypt.Net.BCrypt.HashPassword(
                        "buyer123"
                    ),

                Role = UserRoles.Buyer,

                CreatedAt = DateTime.UtcNow
            });
        }


        // ==========================================
        // SAVE USERS FIRST
        // ==========================================

        await context.SaveChangesAsync();


        // ==========================================
        // SEED SAMPLE SHIPMENTS (Component 3)
        // Only seed if no shipments exist yet
        // ==========================================

        if (!await context.Shipments.AnyAsync())
        {
            var seller = await context.Users.FirstAsync(u => u.Email == sellerEmail);
            var buyer = await context.Users.FirstAsync(u => u.Email == buyerEmail);

            // Sample shipment 1
            var shipment1 = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ShipmentNumber = "SHP-2026-001",
                SellerUserId = seller.Id,
                BuyerUserId = buyer.Id,
                Origin = "Colombo, Sri Lanka",
                Destination = "Kandy, Sri Lanka",
                DeclaredValue = 5000.00m,
                Currency = "USD",
                PackageDescription = "2.5 carat blue sapphire with certificate",
                SelectedService = "Express Courier",
                CourierName = "DHL Express",
                ExternalShipmentReference = null,
                TrackingNumber = null,
                Status = Domain.Enums.ShipmentStatus.Planning,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            };

            // Sample shipment 2
            var shipment2 = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                ShipmentNumber = "SHP-2026-002",
                SellerUserId = seller.Id,
                BuyerUserId = buyer.Id,
                Origin = "Colombo, Sri Lanka",
                Destination = "Galle, Sri Lanka",
                DeclaredValue = 3000.00m,
                Currency = "USD",
                PackageDescription = "1.8 carat ruby ring",
                SelectedService = "Standard Post",
                CourierName = "Sri Lanka Post",
                ExternalShipmentReference = "EXT-REF-002",
                TrackingNumber = "TRK-987654321",
                Status = Domain.Enums.ShipmentStatus.InTransit,
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            };

            context.Shipments.AddRange(shipment1, shipment2);
            await context.SaveChangesAsync();


            // ==========================================
            // SEED SHIPPING PLANS
            // ==========================================

            var plan1 = new ShippingPlan
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment1.Id,
                RiskLevel = Domain.Enums.RiskLevel.Medium,
                RecommendedServiceType = "Express Courier with Insurance",
                InsuranceRecommended = true,
                RecommendedCoverage = 5000.00m,
                Requirements = "[\"Certificate of Authenticity\", \"Export Permit\"]",
                Warnings = "[\"Ensure proper packaging for fragile items\", \"High value requires signature on delivery\"]",
                GeneratedAt = DateTime.UtcNow.AddDays(-5),
                Status = "PendingAdminApproval",
                ApprovedAt = null,
                ApprovedByUserId = null,
                RejectionReason = null
            };

            var plan2 = new ShippingPlan
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment2.Id,
                RiskLevel = Domain.Enums.RiskLevel.Low,
                RecommendedServiceType = "Standard Post with Basic Insurance",
                InsuranceRecommended = true,
                RecommendedCoverage = 3000.00m,
                Requirements = "[\"Invoice\", \"Packing List\"]",
                Warnings = "[]",
                GeneratedAt = DateTime.UtcNow.AddDays(-3),
                Status = "Approved",
                ApprovedAt = DateTime.UtcNow.AddDays(-2),
                ApprovedByUserId = seller.Id,
                RejectionReason = null
            };

            context.ShippingPlans.AddRange(plan1, plan2);
            await context.SaveChangesAsync();


            // ==========================================
            // SEED TRACKING EVENTS FOR SHIPMENT 2
            // ==========================================

            var trackingEvents = new[]
            {
                new ShipmentTrackingEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment2.Id,
                    Status = "Created",
                    LocationText = "Colombo",
                    ExternalEventCode = null,
                    Description = "Shipment created and label generated",
                    OccurredAt = DateTime.UtcNow.AddDays(-3),
                    RecordedAt = DateTime.UtcNow.AddDays(-3)
                },
                new ShipmentTrackingEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment2.Id,
                    Status = "Picked Up",
                    LocationText = "Colombo Distribution Center",
                    ExternalEventCode = null,
                    Description = "Package picked up by courier",
                    OccurredAt = DateTime.UtcNow.AddDays(-2.5),
                    RecordedAt = DateTime.UtcNow.AddDays(-2.5)
                },
                new ShipmentTrackingEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment2.Id,
                    Status = "In Transit",
                    LocationText = "En Route to Galle",
                    ExternalEventCode = null,
                    Description = "Package in transit to destination",
                    OccurredAt = DateTime.UtcNow.AddDays(-1),
                    RecordedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.ShipmentTrackingEvents.AddRange(trackingEvents);
            await context.SaveChangesAsync();


            // ==========================================
            // SEED INSURANCE RECORD FOR SHIPMENT 2
            // ==========================================

            var insurance = new InsuranceRecord
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment2.Id,
                Provider = "Mock Insurance Provider",
                PolicyReference = "INS-2026-001",
                DeclaredValue = 3000.00m,
                CoverageAmount = 3000.00m,
                CoverageType = Domain.Enums.InsuranceCoverageType.Standard,
                Status = Domain.Enums.InsuranceStatus.Active,
                PremiumAmount = 45.00m,
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            };

            context.InsuranceRecords.Add(insurance);
            await context.SaveChangesAsync();
        }
    }
}