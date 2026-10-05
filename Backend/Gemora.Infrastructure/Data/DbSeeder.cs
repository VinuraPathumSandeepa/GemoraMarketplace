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

        var admin =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == adminEmail
                );

        if (admin == null)
        {
            admin = new User
            {
                FullName = "Gemora Admin",
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRoles.Admin,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(admin);
            await context.SaveChangesAsync();
        }
        else
        {
            var adminPasswordMatches =
                BCrypt.Net.BCrypt.Verify(adminPassword, admin.PasswordHash);

            if (!adminPasswordMatches)
            {
                admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
                await context.SaveChangesAsync();
            }
        }


        // ==========================================
        // GEMOLOGIST
        // ==========================================

        var gemologistEmail = "gemologist@gemora.com";

        var gemologist =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == gemologistEmail
                );

        if (gemologist == null)
        {
            gemologist = new User
            {
                FullName = "Gemora Gemologist",
                Email = gemologistEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(gemologistPassword),
                Role = UserRoles.Gemologist,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(gemologist);
            await context.SaveChangesAsync();
        }
        else
        {
            var gemologistPasswordMatches =
                BCrypt.Net.BCrypt.Verify(gemologistPassword, gemologist.PasswordHash);

            if (!gemologistPasswordMatches)
            {
                gemologist.PasswordHash = BCrypt.Net.BCrypt.HashPassword(gemologistPassword);
                await context.SaveChangesAsync();
            }
        }


        // ==========================================
        // EXPORT OFFICER
        // ==========================================

        var exportOfficerEmail = "export@gemora.com";

        var exportOfficer =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == exportOfficerEmail
                );

        if (exportOfficer == null)
        {
            exportOfficer = new User
            {
                FullName = "Gemora Export Officer",
                Email = exportOfficerEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(exportOfficerPassword),
                Role = UserRoles.ExportOfficer,
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(exportOfficer);
            await context.SaveChangesAsync();
        }
        else
        {
            var exportOfficerPasswordMatches =
                BCrypt.Net.BCrypt.Verify(exportOfficerPassword, exportOfficer.PasswordHash);

            if (!exportOfficerPasswordMatches)
            {
                exportOfficer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(exportOfficerPassword);
                await context.SaveChangesAsync();
            }
        }


        // ==========================================
        // TEST BUYER
        // ==========================================

        var buyerEmail = "buyer@gemora.com";

        var buyer =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == buyerEmail
                );

        if (buyer == null)
        {
            buyer = new User
            {
                FullName = "Test Buyer",
                Email = buyerEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("buyer123"),
                Role = UserRoles.Buyer,
                PhoneNumber = "+94771234567",
                CountryCode = "LK",
                Region = "Western Province",
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(buyer);
            await context.SaveChangesAsync();
        }


        // ==========================================
        // TEST SELLER
        // ==========================================

        var sellerEmail = "seller@gemora.com";

        var seller =
            await context.Users
                .FirstOrDefaultAsync(
                    user =>
                        user.Email == sellerEmail
                );

        if (seller == null)
        {
            seller = new User
            {
                FullName = "Test Seller",
                Email = sellerEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("seller123"),
                Role = UserRoles.Seller,
                PhoneNumber = "+94779876543",
                CountryCode = "LK",
                Region = "Central Province",
                IsEmailVerified = true,
                EmailVerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(seller);
            await context.SaveChangesAsync();
        }


        // ==========================================
        // SEED SAMPLE GEM LISTINGS (for test orders)
        // DEVELOPMENT ONLY: Represents Component 2 listings
        // ==========================================

        var existingListings = await context.GemListings
            .Where(g => g.SellerId == seller.Id)
            .ToListAsync();

        int? gemListingId1 = null;
        int? gemListingId2 = null;
        int? gemListingId3 = null;
        int? gemListingId4 = null;
        int? gemListingId5 = null;
        int? gemListingId6 = null;

        if (!existingListings.Any())
        {
            // DEVELOPMENT ONLY: Create 6 test gem listings for the seller covering all demo scenarios
            var gemListing1 = new GemListing
            {
                Title = "Natural Blue Sapphire",
                Description = "Premium quality blue sapphire from Sri Lanka",
                Price = 850000m,
                Currency = "LKR",
                CaratWeight = 2.5m,
                Color = "Blue",
                Clarity = "VVS1",
                Cut = "Oval",
                GemType = "Sapphire",
                CountryCode = "LK",
                Region = "Sabaragamuwa Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            };

            var gemListing2 = new GemListing
            {
                Title = "Ceylon Ruby",
                Description = "Beautiful ruby with excellent clarity",
                Price = 620000m,
                Currency = "LKR",
                CaratWeight = 1.8m,
                Color = "Red",
                Clarity = "VS1",
                Cut = "Round",
                GemType = "Ruby",
                CountryCode = "LK",
                Region = "Central Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-9)
            };

            var gemListing3 = new GemListing
            {
                Title = "Yellow Sapphire",
                Description = "Brilliant yellow sapphire with certification",
                Price = 1100000m,
                Currency = "LKR",
                CaratWeight = 3.0m,
                Color = "Yellow",
                Clarity = "VVS2",
                Cut = "Cushion",
                GemType = "Sapphire",
                CountryCode = "LK",
                Region = "Southern Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            };

            var gemListing4 = new GemListing
            {
                Title = "Padparadscha Sapphire",
                Description = "Rare padparadscha sapphire with unique pink-orange hue",
                Price = 1450000m,
                Currency = "LKR",
                CaratWeight = 2.2m,
                Color = "Pink-Orange",
                Clarity = "VVS1",
                Cut = "Oval",
                GemType = "Sapphire",
                CountryCode = "LK",
                Region = "Western Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            };

            var gemListing5 = new GemListing
            {
                Title = "Alexandrite",
                Description = "Color-changing alexandrite - rare collector's stone",
                Price = 1750000m,
                Currency = "LKR",
                CaratWeight = 1.5m,
                Color = "Green-Purple",
                Clarity = "VS1",
                Cut = "Round",
                GemType = "Alexandrite",
                CountryCode = "LK",
                Region = "Eastern Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            };

            var gemListing6 = new GemListing
            {
                Title = "Blue Spinel",
                Description = "High-quality blue spinel with exceptional brilliance",
                Price = 480000m,
                Currency = "LKR",
                CaratWeight = 2.0m,
                Color = "Blue",
                Clarity = "VVS2",
                Cut = "Cushion",
                GemType = "Spinel",
                CountryCode = "LK",
                Region = "North Central Province",
                SellerId = seller.Id,
                Status = "Available",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            };

            context.GemListings.AddRange(gemListing1, gemListing2, gemListing3, gemListing4, gemListing5, gemListing6);
            await context.SaveChangesAsync();

            gemListingId1 = gemListing1.Id;
            gemListingId2 = gemListing2.Id;
            gemListingId3 = gemListing3.Id;
            gemListingId4 = gemListing4.Id;
            gemListingId5 = gemListing5.Id;
            gemListingId6 = gemListing6.Id;
        }
        else
        {
            // Reuse existing listings (take up to 6)
            gemListingId1 = existingListings[0].Id;
            gemListingId2 = existingListings.Count > 1 ? existingListings[1].Id : existingListings[0].Id;
            gemListingId3 = existingListings.Count > 2 ? existingListings[2].Id : existingListings[0].Id;
            gemListingId4 = existingListings.Count > 3 ? existingListings[3].Id : existingListings[0].Id;
            gemListingId5 = existingListings.Count > 4 ? existingListings[4].Id : existingListings[0].Id;
            gemListingId6 = existingListings.Count > 5 ? existingListings[5].Id : existingListings[0].Id;
        }


        // ==========================================
        // SEED COMPONENT 3 DEMO SCENARIOS
        // DEVELOPMENT ONLY: Demo scenarios for Component 3 Secure Shipping & Insurance.
        // Component 2 will eventually supply paid orders.
        // Component 4 will eventually supply export-clearance results.
        // All external courier, insurance, and export data in these demo scenarios is simulated development data.
        // ==========================================

        // Demo rebuilding deletes existing seller shipments and must be explicitly enabled.
        // Normal startup only seeds accounts/listings, so optional demo failures cannot block login.
        if (!bool.TryParse(configuration["SeedUsers:EnableShippingDemoReset"], out var enableShippingDemoReset)
            || !enableShippingDemoReset)
        {
            return;
        }

        // Check if we've already seeded ALL demo scenarios by looking for Scenario A marker order AND its shipment
        // This makes the seeder IDEMPOTENT - won't duplicate on repeated runs
        // Must check for BOTH orders and shipments since Program.cs drops shipments but keeps orders
        var hasDemoOrders = await context.Orders
            .AnyAsync(o => o.BuyerId == buyer.Id && o.SellerId == seller.Id && 
                          o.Status == "Paid" && o.TotalAmount == 850000m && 
                          o.ShippingAddress == "45 Galle Road, Colombo 03");
        
        var hasScenarioBShipment = await context.Shipments
            .AnyAsync(s => s.SellerId == seller.Id && s.TrackingNumber == null && s.Status == "Planning");
        
        // Only skip if we have BOTH orders AND at least one shipment (meaning full seed completed)
        if (hasDemoOrders && hasScenarioBShipment)
        {
            Console.WriteLine("Component 3 demo scenarios already seeded. Skipping.");
            return;
        }
        
        // If we have orders but no shipments, Program.cs dropped the shipments - need to reseed
        if (hasDemoOrders && !hasScenarioBShipment)
        {
            Console.WriteLine("Demo orders exist but shipments were dropped. Reseeding Component 3 scenarios...");
        }

        Console.WriteLine("Seeding Component 3 demo scenarios...");

        // Clean up any existing partial demo data in CORRECT DEPENDENCY ORDER
        // Must delete children before parents to respect FK constraints
        
        // Step 1: Delete tracking events for seller's shipments
        var existingShipmentIds = context.Shipments
            .Where(s => s.SellerId == seller.Id)
            .Select(s => s.Id)
            .ToList();
        
        if (existingShipmentIds.Any())
        {
            var trackingEvents = context.ShipmentTrackingEvents
                .Where(t => existingShipmentIds.Contains(t.ShipmentId))
                .ToList();
            context.ShipmentTrackingEvents.RemoveRange(trackingEvents);
            
            // Step 2: Delete insurance records
            var insurances = context.InsuranceRecords
                .Where(i => existingShipmentIds.Contains(i.ShipmentId))
                .ToList();
            context.InsuranceRecords.RemoveRange(insurances);
            
            // Step 3: Delete shipping plans
            var plans = context.ShippingPlans
                .Where(p => existingShipmentIds.Contains(p.ShipmentId))
                .ToList();
            context.ShippingPlans.RemoveRange(plans);
            
            // Step 4: Delete shipments
            var shipments = context.Shipments
                .Where(s => existingShipmentIds.Contains(s.Id))
                .ToList();
            context.Shipments.RemoveRange(shipments);
            
            await context.SaveChangesAsync();
            Console.WriteLine($"Cleaned up {trackingEvents.Count} tracking events, {insurances.Count} insurance records, {plans.Count} plans, {shipments.Count} shipments");
        }
        
        // ==========================================
        // PHASE 1: Create or reuse all 6 orders first (dependency for shipments)
        // ==========================================
        
        // Check if demo orders already exist (from previous run where Program.cs kept Orders table)
        var existingDemoOrders = context.Orders
            .Where(o => o.BuyerId == buyer.Id && o.SellerId == seller.Id && 
                       o.Status == "Paid" && o.Currency == "LKR")
            .OrderBy(o => o.CreatedAt)
            .ToList();
        
        Order orderA, orderB, orderC, orderD, orderE, orderF;
        
        if (existingDemoOrders.Count >= 6)
        {
            // Reuse existing orders to maintain referential integrity
            orderA = existingDemoOrders[0];
            orderB = existingDemoOrders[1];
            orderC = existingDemoOrders[2];
            orderD = existingDemoOrders[3];
            orderE = existingDemoOrders[4];
            orderF = existingDemoOrders[5];
            Console.WriteLine($"Reusing {existingDemoOrders.Count} existing demo orders");
        }
        else
        {
            // Clean up any partial orders before creating new ones
            if (existingDemoOrders.Any())
            {
                context.Orders.RemoveRange(existingDemoOrders);
                await context.SaveChangesAsync();
                Console.WriteLine($"Removed {existingDemoOrders.Count} partial demo orders");
            }
            
            // Create new orders
            orderA = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId1,
                TotalAmount = 850000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "45 Galle Road, Colombo 03",
                ShippingRegion = "Western Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-7),
                CreatedAt = DateTime.UtcNow.AddDays(-10),
                UpdatedAt = DateTime.UtcNow
            };

            orderB = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId2,
                TotalAmount = 620000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "18 Peradeniya Road, Kandy",
                ShippingRegion = "Central Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-6),
                CreatedAt = DateTime.UtcNow.AddDays(-9),
                UpdatedAt = DateTime.UtcNow
            };

            orderC = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId3,
                TotalAmount = 1100000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "72 Matara Road, Galle",
                ShippingRegion = "Southern Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-8),
                UpdatedAt = DateTime.UtcNow
            };

            orderD = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId4,
                TotalAmount = 1450000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "120 Temple Road, Negombo",
                ShippingRegion = "Western Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-4),
                CreatedAt = DateTime.UtcNow.AddDays(-7),
                UpdatedAt = DateTime.UtcNow
            };

            orderE = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId5,
                TotalAmount = 1750000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "55 Beach Road, Trincomalee",
                ShippingRegion = "Eastern Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-3),
                CreatedAt = DateTime.UtcNow.AddDays(-6),
                UpdatedAt = DateTime.UtcNow
            };

            orderF = new Order
            {
                Id = Guid.NewGuid(),
                BuyerId = buyer.Id,
                SellerId = seller.Id,
                GemListingId = gemListingId6,
                TotalAmount = 480000m,
                Currency = "LKR",
                Status = "Paid",
                ShippingAddress = "88 Main Street, Jaffna",
                ShippingRegion = "Northern Province",
                ShippingCountryCode = "LK",
                PaidAt = DateTime.UtcNow.AddDays(-2),
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                UpdatedAt = DateTime.UtcNow
            };

            context.Orders.AddRange(orderA, orderB, orderC, orderD, orderE, orderF);
            await context.SaveChangesAsync();
            Console.WriteLine("Created 6 new demo orders (A-F)");
        }


        // ==========================================
        // SCENARIO A — FRESH PAID ORDER
        // Purpose: Seller can create shipment from this order
        // No shipment exists yet - orderA is intentionally left without a shipment
        // ==========================================


        // ==========================================
        // SCENARIO B — PLAN WAITING FOR ADMIN
        // Paid order + shipment in Planning status
        // ShippingPlan generated but not approved
        // ==========================================

        var shipmentB = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderB.Id,
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            OriginAddress = "Seller Warehouse, Colombo",
            OriginRegion = "Western Province",
            OriginCountryCode = "LK",
            DestinationAddress = orderB.ShippingAddress,
            DestinationRegion = orderB.ShippingRegion,
            DestinationCountryCode = orderB.ShippingCountryCode,
            DeclaredValue = orderB.TotalAmount,
            Currency = orderB.Currency,
            PackageDescription = "1.8 carat Ceylon Ruby with certificate",
            PackageWeight = 0.5m,
            SpecialHandlingNotes = "Handle with care - fragile gemstone",
            PreferredService = "Insured Express",
            ExportRequired = false,
            Status = "Planning",
            RiskLevel = "Medium",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow
        };

        context.Shipments.Add(shipmentB);
        await context.SaveChangesAsync();

        var planB = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentB.Id,
            RiskLevel = "Medium",
            RiskReasons = "Moderate value gemstone requiring secure handling",
            RecommendedServiceType = "Insured Express",
            InsuranceRecommended = true,
            RecommendedCoverageAmount = 620000m,
            HandlingRequirements = "Tamper-evident packaging; Signature on delivery; Secure gemstone container",
            RequiredDocuments = "Certificate of Authenticity; Invoice",
            Warnings = "High-value gemstone; Verify recipient identity at handover",
            GenerationSource = "FallbackRules",
            ExecutionSummary = "Deterministic analysis: risk=Medium, service=Insured Express",
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            UpdatedAt = DateTime.UtcNow,
            IsApproved = false
        };

        context.ShippingPlans.Add(planB);
        await context.SaveChangesAsync();
        Console.WriteLine("Scenario B: Plan waiting for admin approval");


        // ==========================================
        // SCENARIO C — APPROVED / READY FOR BOOKING
        // Plan generated and admin approved
        // Ready for courier booking
        // ==========================================

        var shipmentC = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderC.Id,
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            OriginAddress = "Seller Warehouse, Colombo",
            OriginRegion = "Western Province",
            OriginCountryCode = "LK",
            DestinationAddress = orderC.ShippingAddress,
            DestinationRegion = orderC.ShippingRegion,
            DestinationCountryCode = orderC.ShippingCountryCode,
            DeclaredValue = orderC.TotalAmount,
            Currency = orderC.Currency,
            PackageDescription = "3.0 carat Yellow Sapphire with certification",
            PackageWeight = 0.6m,
            SpecialHandlingNotes = "High-value item - restricted access handling",
            PreferredService = "High-Value Insured Express",
            ExportRequired = true,
            Status = "ReadyForBooking",
            RiskLevel = "High",
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UpdatedAt = DateTime.UtcNow
        };

        context.Shipments.Add(shipmentC);
        await context.SaveChangesAsync();

        var planC = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentC.Id,
            RiskLevel = "High",
            RiskReasons = "High declared value requires enhanced security measures",
            RecommendedServiceType = "High-Value Insured Express",
            InsuranceRecommended = true,
            RecommendedCoverageAmount = 1100000m,
            HandlingRequirements = "Tamper-evident package; Declared-value handling; Signature required; Restricted handover; Tracking mandatory",
            RequiredDocuments = "Certificate of Authenticity; Export Certificate; Commercial Invoice; Packing List",
            Warnings = "High declared value; Manual Admin review required",
            GenerationSource = "AI",
            ExecutionSummary = "AI analysis: 3 risk factors identified, 5 handling requirements, 4 documents needed",
            CreatedAt = DateTime.UtcNow.AddDays(-4),
            UpdatedAt = DateTime.UtcNow.AddDays(-3),
            IsApproved = true,
            ApprovedBy = admin.Id,
            ApprovedAt = DateTime.UtcNow.AddDays(-3),
            AdminNotes = "Approved for high-value shipment with full insurance coverage"
        };

        context.ShippingPlans.Add(planC);
        await context.SaveChangesAsync();
        Console.WriteLine("Scenario C: Approved ready for booking");


        // ==========================================
        // SCENARIO D — BOOKED + INSURED + IN TRANSIT
        // Admin approved, courier booked, insurance recorded
        // Package picked up and now InTransit
        // ==========================================

        var shipmentD = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderD.Id,
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            OriginAddress = "Seller Warehouse, Colombo",
            OriginRegion = "Western Province",
            OriginCountryCode = "LK",
            DestinationAddress = orderD.ShippingAddress,
            DestinationRegion = orderD.ShippingRegion,
            DestinationCountryCode = orderD.ShippingCountryCode,
            DeclaredValue = orderD.TotalAmount,
            Currency = orderD.Currency,
            PackageDescription = "2.2 carat Padparadscha Sapphire - rare pink-orange stone",
            PackageWeight = 0.55m,
            SpecialHandlingNotes = "Extremely rare gemstone - maximum security required",
            PreferredService = "High-Value Insured Express",
            ExportRequired = true,
            Status = "InTransit",
            RiskLevel = "High",
            TrackingNumber = "SIM-TRK-0001",
            CourierName = "DEMO Gemora Courier Sandbox",
            ExternalShipmentReference = "SIM-BOOK-0001",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            BookedAt = DateTime.UtcNow.AddDays(-3),
            ShippedAt = DateTime.UtcNow.AddDays(-2)
        };

        context.Shipments.Add(shipmentD);
        await context.SaveChangesAsync();

        var planD = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentD.Id,
            RiskLevel = "High",
            RiskReasons = "Rare padparadscha sapphire with exceptional market value",
            RecommendedServiceType = "High-Value Insured Express",
            InsuranceRecommended = true,
            RecommendedCoverageAmount = 1450000m,
            HandlingRequirements = "Maximum security packaging; Armored transport; Dual signature required; GPS tracking; Temperature-controlled environment",
            RequiredDocuments = "Certificate of Authenticity; Export Certificate; Appraisal Certificate; Commercial Invoice",
            Warnings = "Exceptionally rare gemstone; Irreplaceable collector's item; Enhanced chain of custody required",
            GenerationSource = "AI",
            ExecutionSummary = "AI analysis: 4 risk factors identified, 5 handling requirements, 4 documents needed",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            UpdatedAt = DateTime.UtcNow.AddDays(-2),
            IsApproved = true,
            ApprovedBy = admin.Id,
            ApprovedAt = DateTime.UtcNow.AddDays(-2),
            AdminNotes = "Approved with maximum security protocols"
        };

        context.ShippingPlans.Add(planD);
        await context.SaveChangesAsync();

        var insuranceD = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentD.Id,
            DeclaredValue = orderD.TotalAmount,
            CoverageAmount = 1450000m,
            Currency = "LKR",
            CoverageType = "Full Declared Value",
            PolicyNumber = "SIM-POL-0001",
            PolicyReference = "DEMO-GEMORA-INS-0001",
            ProviderName = "DEMO Gemora Insurance Sandbox",
            PremiumAmount = 14500m, // 1% of coverage
            PolicyStartDate = DateTime.UtcNow.AddDays(-2),
            PolicyEndDate = DateTime.UtcNow.AddDays(28),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-2)
        };

        context.InsuranceRecords.Add(insuranceD);
        await context.SaveChangesAsync();

        // Tracking timeline for Scenario D
        var trackingEventsD = new[]
        {
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentD.Id,
                EventType = "Booked",
                Location = "Colombo",
                Description = "Courier booking confirmed in demo sandbox",
                ExternalEventCode = "SIM-EVT-BOOK-0001",
                OccurredAt = DateTime.UtcNow.AddDays(-3),
                RecordedAt = DateTime.UtcNow.AddDays(-3)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentD.Id,
                EventType = "PickedUp",
                Location = "Colombo",
                Description = "Shipment collected from Seller warehouse",
                ExternalEventCode = "SIM-EVT-PICKUP-0001",
                OccurredAt = DateTime.UtcNow.AddDays(-2.5),
                RecordedAt = DateTime.UtcNow.AddDays(-2.5)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentD.Id,
                EventType = "InTransit",
                Location = "Colombo Distribution Centre",
                Description = "Shipment is moving through secure courier network",
                ExternalEventCode = "SIM-EVT-TRANSIT-0001",
                OccurredAt = DateTime.UtcNow.AddDays(-1),
                RecordedAt = DateTime.UtcNow.AddDays(-1)
            }
        };

        context.ShipmentTrackingEvents.AddRange(trackingEventsD);
        await context.SaveChangesAsync();
        Console.WriteLine("Scenario D: Booked + insured + in transit");


        // ==========================================
        // SCENARIO E — DELIVERY EXCEPTION
        // Exception case for testing failed delivery workflows
        // ==========================================

        var shipmentE = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderE.Id,
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            OriginAddress = "Seller Warehouse, Colombo",
            OriginRegion = "Western Province",
            OriginCountryCode = "LK",
            DestinationAddress = orderE.ShippingAddress,
            DestinationRegion = orderE.ShippingRegion,
            DestinationCountryCode = orderE.ShippingCountryCode,
            DeclaredValue = orderE.TotalAmount,
            Currency = orderE.Currency,
            PackageDescription = "1.5 carat Alexandrite - color-changing collector's stone",
            PackageWeight = 0.45m,
            SpecialHandlingNotes = "Ultra-rare gemstone - requires secure holding facility",
            PreferredService = "Premium Insured Express",
            ExportRequired = true,
            Status = "Exception",
            RiskLevel = "Critical",
            TrackingNumber = "SIM-TRK-0002",
            CourierName = "DEMO Gemora Courier Sandbox",
            ExternalShipmentReference = "SIM-BOOK-0002",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow,
            BookedAt = DateTime.UtcNow.AddDays(-2),
            ShippedAt = DateTime.UtcNow.AddDays(-1.5)
        };

        context.Shipments.Add(shipmentE);
        await context.SaveChangesAsync();

        var planE = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentE.Id,
            RiskLevel = "Critical",
            RiskReasons = "Ultra-rare alexandrite with highest market value and irreplaceable nature",
            RecommendedServiceType = "Premium Insured Express with Maximum Security",
            InsuranceRecommended = true,
            RecommendedCoverageAmount = 1750000m,
            HandlingRequirements = "Military-grade packaging; Armed escort; Biometric verification; Real-time GPS tracking; Climate-controlled vault storage",
            RequiredDocuments = "Certificate of Authenticity; Export Certificate; GIA Appraisal; Insurance Certificate; Chain of Custody Documentation",
            Warnings = "Highest value item in system; Irreplaceable museum-quality piece; Maximum liability exposure",
            GenerationSource = "AI",
            ExecutionSummary = "AI analysis: 5 risk factors identified, 5 handling requirements, 5 documents needed",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            IsApproved = true,
            ApprovedBy = admin.Id,
            ApprovedAt = DateTime.UtcNow.AddDays(-1),
            AdminNotes = "Approved with critical-level security protocols"
        };

        context.ShippingPlans.Add(planE);
        await context.SaveChangesAsync();

        var insuranceE = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentE.Id,
            DeclaredValue = orderE.TotalAmount,
            CoverageAmount = 1750000m,
            Currency = "LKR",
            CoverageType = "Comprehensive",
            PolicyNumber = "SIM-POL-0002",
            PolicyReference = "DEMO-GEMORA-INS-0002",
            ProviderName = "DEMO Gemora Insurance Sandbox",
            PremiumAmount = 26250m, // 1.5% of coverage (higher for critical risk)
            PolicyStartDate = DateTime.UtcNow.AddDays(-1),
            PolicyEndDate = DateTime.UtcNow.AddDays(29),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        context.InsuranceRecords.Add(insuranceE);
        await context.SaveChangesAsync();

        // Tracking timeline for Scenario E - includes exception
        var trackingEventsE = new[]
        {
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentE.Id,
                EventType = "Booked",
                Location = "Colombo",
                Description = "Courier booking confirmed in demo sandbox",
                ExternalEventCode = "SIM-EVT-BOOK-0002",
                OccurredAt = DateTime.UtcNow.AddDays(-2),
                RecordedAt = DateTime.UtcNow.AddDays(-2)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentE.Id,
                EventType = "PickedUp",
                Location = "Colombo",
                Description = "Shipment collected from Seller warehouse",
                ExternalEventCode = "SIM-EVT-PICKUP-0002",
                OccurredAt = DateTime.UtcNow.AddDays(-1.8),
                RecordedAt = DateTime.UtcNow.AddDays(-1.8)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentE.Id,
                EventType = "InTransit",
                Location = "Kandy Distribution Hub",
                Description = "Shipment in transit via secure armored transport",
                ExternalEventCode = "SIM-EVT-TRANSIT-0002",
                OccurredAt = DateTime.UtcNow.AddDays(-1),
                RecordedAt = DateTime.UtcNow.AddDays(-1)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentE.Id,
                EventType = "OutForDelivery",
                Location = "Trincomalee",
                Description = "Shipment out for delivery to verified recipient",
                ExternalEventCode = "SIM-EVT-OFD-0002",
                OccurredAt = DateTime.UtcNow.AddHours(-6),
                RecordedAt = DateTime.UtcNow.AddHours(-6)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentE.Id,
                EventType = "Exception",
                Location = "Trincomalee",
                Description = "Recipient unavailable at secure handover. Shipment moved to secure holding facility pending Admin action.",
                ExternalEventCode = "SIM-EVT-EXCEPTION-0002",
                OccurredAt = DateTime.UtcNow.AddHours(-3),
                RecordedAt = DateTime.UtcNow.AddHours(-3)
            }
        };

        context.ShipmentTrackingEvents.AddRange(trackingEventsE);
        await context.SaveChangesAsync();
        Console.WriteLine("Scenario E: Delivery exception");


        // ==========================================
        // SCENARIO F — COMPLETED DELIVERY
        // Successfully delivered shipment with complete tracking history
        // ==========================================

        var shipmentF = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderF.Id,
            SellerId = seller.Id,
            BuyerId = buyer.Id,
            OriginAddress = "Seller Warehouse, Colombo",
            OriginRegion = "Western Province",
            OriginCountryCode = "LK",
            DestinationAddress = orderF.ShippingAddress,
            DestinationRegion = orderF.ShippingRegion,
            DestinationCountryCode = orderF.ShippingCountryCode,
            DeclaredValue = orderF.TotalAmount,
            Currency = orderF.Currency,
            PackageDescription = "2.0 carat Blue Spinel with exceptional brilliance",
            PackageWeight = 0.5m,
            SpecialHandlingNotes = "Standard secure handling procedures",
            PreferredService = "Insured Express",
            ExportRequired = false,
            Status = "Delivered",
            RiskLevel = "Medium",
            TrackingNumber = "SIM-TRK-0003",
            CourierName = "DEMO Gemora Courier Sandbox",
            ExternalShipmentReference = "SIM-BOOK-0003",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-6),
            BookedAt = DateTime.UtcNow.AddDays(-1),
            ShippedAt = DateTime.UtcNow.AddHours(-18),
            DeliveredAt = DateTime.UtcNow.AddHours(-6)
        };

        context.Shipments.Add(shipmentF);
        await context.SaveChangesAsync();

        var planF = new ShippingPlan
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentF.Id,
            RiskLevel = "Medium",
            RiskReasons = "Moderate value blue spinel requiring standard insurance coverage",
            RecommendedServiceType = "Insured Express",
            InsuranceRecommended = true,
            RecommendedCoverageAmount = 480000m,
            HandlingRequirements = "Secure packaging; Signature on delivery; Standard tracking",
            RequiredDocuments = "Certificate of Authenticity; Invoice",
            GenerationSource = "FallbackRules",
            ExecutionSummary = "Deterministic analysis: risk=Medium, service=Insured Express",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddHours(-20),
            IsApproved = true,
            ApprovedBy = admin.Id,
            ApprovedAt = DateTime.UtcNow.AddHours(-20),
            AdminNotes = "Approved for standard insured delivery"
        };

        context.ShippingPlans.Add(planF);
        await context.SaveChangesAsync();

        var insuranceF = new InsuranceRecord
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentF.Id,
            DeclaredValue = orderF.TotalAmount,
            CoverageAmount = 480000m,
            Currency = "LKR",
            CoverageType = "Standard",
            PolicyNumber = "SIM-POL-0003",
            PolicyReference = "DEMO-GEMORA-INS-0003",
            ProviderName = "DEMO Gemora Insurance Sandbox",
            PremiumAmount = 4800m, // 1% of coverage
            PolicyStartDate = DateTime.UtcNow.AddHours(-20),
            PolicyEndDate = DateTime.UtcNow.AddDays(20),
            Status = "Active",
            CreatedAt = DateTime.UtcNow.AddHours(-20),
            UpdatedAt = DateTime.UtcNow.AddHours(-20)
        };

        context.InsuranceRecords.Add(insuranceF);
        await context.SaveChangesAsync();

        // Complete tracking timeline for Scenario F
        var trackingEventsF = new[]
        {
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentF.Id,
                EventType = "Booked",
                Location = "Colombo",
                Description = "Courier booking confirmed in demo sandbox",
                ExternalEventCode = "SIM-EVT-BOOK-0003",
                OccurredAt = DateTime.UtcNow.AddDays(-1),
                RecordedAt = DateTime.UtcNow.AddDays(-1)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentF.Id,
                EventType = "PickedUp",
                Location = "Colombo",
                Description = "Shipment collected from Seller warehouse",
                ExternalEventCode = "SIM-EVT-PICKUP-0003",
                OccurredAt = DateTime.UtcNow.AddHours(-22),
                RecordedAt = DateTime.UtcNow.AddHours(-22)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentF.Id,
                EventType = "InTransit",
                Location = "Kurunegala Hub",
                Description = "Shipment in transit to Northern Province",
                ExternalEventCode = "SIM-EVT-TRANSIT-0003",
                OccurredAt = DateTime.UtcNow.AddHours(-16),
                RecordedAt = DateTime.UtcNow.AddHours(-16)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentF.Id,
                EventType = "OutForDelivery",
                Location = "Jaffna",
                Description = "Shipment out for final delivery",
                ExternalEventCode = "SIM-EVT-OFD-0003",
                OccurredAt = DateTime.UtcNow.AddHours(-8),
                RecordedAt = DateTime.UtcNow.AddHours(-8)
            },
            new ShipmentTrackingEvent
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentF.Id,
                EventType = "Delivered",
                Location = "Jaffna",
                Description = "Delivered to verified recipient. Shipment completed successfully.",
                ExternalEventCode = "SIM-EVT-DELIVERED-0003",
                OccurredAt = DateTime.UtcNow.AddHours(-6),
                RecordedAt = DateTime.UtcNow.AddHours(-6)
            }
        };

        context.ShipmentTrackingEvents.AddRange(trackingEventsF);
        await context.SaveChangesAsync();
        Console.WriteLine("Scenario F: Completed delivery");
        
        Console.WriteLine("Component 3 demo scenarios seeded successfully!");
    }
}
