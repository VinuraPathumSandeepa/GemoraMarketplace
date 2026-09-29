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
        // SEED SAMPLE ORDERS (Component 3)
        // Only seed if no orders exist yet
        // ==========================================

        var seller = await context.Users.FirstAsync(u => u.Email == sellerEmail);
        var buyer = await context.Users.FirstAsync(u => u.Email == buyerEmail);

        if (!await context.Orders.AnyAsync())
        {
            // Paid order 1 - eligible for shipment
            var order1 = new Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                TotalAmount = 5000.00m,
                Currency = "USD",
                Status = "Paid",
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow
            };

            // Paid order 2 - eligible for shipment
            var order2 = new Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                TotalAmount = 3000.00m,
                Currency = "USD",
                Status = "Paid",
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow
            };

            // Pending order - NOT eligible for shipment
            var order3 = new Domain.Entities.Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                TotalAmount = 2000.00m,
                Currency = "USD",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            };

            context.Orders.AddRange(order1, order2, order3);
            await context.SaveChangesAsync();
        }


        // ==========================================
        // SEED SAMPLE SHIPMENTS (Component 3)
        // Only seed if no shipments exist yet
        // ==========================================

        if (!await context.Shipments.AnyAsync())
        {
            // Get the first paid order for shipment 1
            var paidOrder1 = await context.Orders
                .Where(o => o.Status == "Paid")
                .OrderBy(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            // Sample shipment 1 (linked to real order)
            var shipment1 = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = paidOrder1?.Id ?? Guid.NewGuid(),
                SellerId = seller.Id,
                BuyerId = buyer.Id,
                OriginAddress = "Colombo, Sri Lanka",
                DestinationAddress = "Kandy, Sri Lanka",
                DeclaredValue = paidOrder1?.TotalAmount ?? 5000.00m,
                Currency = paidOrder1?.Currency ?? "USD",
                PackageDescription = "2.5 carat blue sapphire with certificate",
                PreferredService = "Express Courier",
                Status = "Planning",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            };

            // Sample shipment 2
            var shipment2 = new Shipment
            {
                Id = Guid.NewGuid(),
                OrderId = Guid.NewGuid(),
                SellerId = seller.Id,
                BuyerId = buyer.Id,
                OriginAddress = "Colombo, Sri Lanka",
                DestinationAddress = "Galle, Sri Lanka",
                DeclaredValue = 3000.00m,
                Currency = "USD",
                PackageDescription = "1.8 carat ruby ring",
                PreferredService = "Standard Post",
                TrackingNumber = "TRK-987654321",
                Status = "InTransit",
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
                RiskLevel = "Medium",
                RecommendedServiceType = "Express Courier with Insurance",
                InsuranceRecommended = true,
                RecommendedCoverageAmount = 5000.00m,
                HandlingRequirements = "[\"Certificate of Authenticity\", \"Export Permit\"]",
                Warnings = "[\"Ensure proper packaging for fragile items\", \"High value requires signature on delivery\"]",
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                IsApproved = false
            };

            var plan2 = new ShippingPlan
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipment2.Id,
                RiskLevel = "Low",
                RecommendedServiceType = "Standard Post with Basic Insurance",
                InsuranceRecommended = true,
                RecommendedCoverageAmount = 3000.00m,
                HandlingRequirements = "[\"Invoice\", \"Packing List\"]",
                Warnings = "[]",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                IsApproved = true,
                ApprovedBy = seller.Id,
                ApprovedAt = DateTime.UtcNow.AddDays(-2)
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
                    EventType = "Created",
                    Location = "Colombo",
                    Description = "Shipment created and label generated",
                    EventTimestamp = DateTime.UtcNow.AddDays(-3),
                    CreatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new ShipmentTrackingEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment2.Id,
                    EventType = "Picked Up",
                    Location = "Colombo Distribution Center",
                    Description = "Package picked up by courier",
                    EventTimestamp = DateTime.UtcNow.AddDays(-2.5),
                    CreatedAt = DateTime.UtcNow.AddDays(-2.5)
                },
                new ShipmentTrackingEvent
                {
                    Id = Guid.NewGuid(),
                    ShipmentId = shipment2.Id,
                    EventType = "In Transit",
                    Location = "En Route to Galle",
                    Description = "Package in transit to destination",
                    EventTimestamp = DateTime.UtcNow.AddDays(-1),
                    CreatedAt = DateTime.UtcNow.AddDays(-1)
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
                ProviderName = "Mock Insurance Provider",
                PolicyNumber = "INS-2026-001",
                CoverageAmount = 3000.00m,
                CoverageType = "Standard",
                Status = "Active",
                Currency = "USD",
                CreatedAt = DateTime.UtcNow.AddDays(-3),
                UpdatedAt = DateTime.UtcNow
            };

            context.InsuranceRecords.Add(insurance);
            await context.SaveChangesAsync();
        }
    }
}