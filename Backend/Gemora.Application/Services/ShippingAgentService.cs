using Gemora.Application.DTOs;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public interface IShippingAgentService
{
    Task<ShippingPlanResponseDto> GenerateShippingPlanAsync(Guid shipmentId);
    Task<bool> ApproveShippingPlanAsync(Guid shipmentId, Guid adminId, string? adminNotes);
}

public class ShippingAgentService : IShippingAgentService
{
    private readonly ApplicationDbContext _context;

    public ShippingAgentService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ShippingPlanResponseDto> GenerateShippingPlanAsync(Guid shipmentId)
    {
        var shipment = await _context.Shipments
            .Include(s => s.Seller)
            .Include(s => s.Buyer)
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // Get order details
        var order = await _context.Orders
            .Include(o => o.GemListing)
            .FirstOrDefaultAsync(o => o.Id == shipment.OrderId);

        if (order == null)
        {
            throw new InvalidOperationException("Associated order not found.");
        }

        // AI Analysis - Deterministic risk assessment based on rules
        var riskLevel = AssessRiskLevel(shipment, order);
        var riskReasons = GenerateRiskReasons(shipment, order, riskLevel);
        var recommendedService = DetermineServiceType(shipment, riskLevel);
        var insuranceRecommended = ShouldRecommendInsurance(shipment.DeclaredValue, riskLevel);
        var coverageAmount = CalculateRecommendedCoverage(shipment.DeclaredValue, riskLevel);
        var handlingRequirements = GenerateHandlingRequirements(riskLevel, shipment.ExportRequired);
        var requiredDocuments = GenerateRequiredDocuments(shipment.ExportRequired, order);
        var warnings = GenerateWarnings(shipment, riskLevel);

        // Check if plan already exists
        var existingPlan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (existingPlan != null)
        {
            // Update existing plan
            existingPlan.RiskLevel = riskLevel;
            existingPlan.RiskReasons = riskReasons;
            existingPlan.RecommendedServiceType = recommendedService;
            existingPlan.InsuranceRecommended = insuranceRecommended;
            existingPlan.RecommendedCoverageAmount = coverageAmount;
            existingPlan.HandlingRequirements = handlingRequirements;
            existingPlan.RequiredDocuments = requiredDocuments;
            existingPlan.Warnings = warnings;
            existingPlan.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new plan
            var plan = new ShippingPlan
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                RiskLevel = riskLevel,
                RiskReasons = riskReasons,
                RecommendedServiceType = recommendedService,
                InsuranceRecommended = insuranceRecommended,
                RecommendedCoverageAmount = coverageAmount,
                HandlingRequirements = handlingRequirements,
                RequiredDocuments = requiredDocuments,
                Warnings = warnings,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShippingPlans.Add(plan);
        }

        await _context.SaveChangesAsync();

        // Update shipment risk level
        shipment.RiskLevel = riskLevel;
        if (shipment.Status == "Pending")
        {
            shipment.Status = "PlanGenerated";
        }
        shipment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var savedPlan = existingPlan ?? await _context.ShippingPlans.FirstAsync(p => p.ShipmentId == shipmentId);

        return MapToResponseDto(savedPlan);
    }

    public async Task<bool> ApproveShippingPlanAsync(Guid shipmentId, Guid adminId, string? adminNotes)
    {
        var plan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (plan == null)
        {
            throw new InvalidOperationException("Shipping plan not found.");
        }

        if (plan.IsApproved)
        {
            throw new InvalidOperationException("Shipping plan is already approved.");
        }

        plan.IsApproved = true;
        plan.ApprovedBy = adminId;
        plan.ApprovedAt = DateTime.UtcNow;
        plan.AdminNotes = adminNotes;
        plan.UpdatedAt = DateTime.UtcNow;

        // Update shipment status
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment != null && shipment.Status == "PlanGenerated")
        {
            shipment.Status = "PlanApproved";
            shipment.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return true;
    }

    // Private helper methods for deterministic AI analysis

    private string AssessRiskLevel(Shipment shipment, Order order)
    {
        int riskScore = 0;

        // High value items increase risk
        if (shipment.DeclaredValue > 10000) riskScore += 3;
        else if (shipment.DeclaredValue > 5000) riskScore += 2;
        else if (shipment.DeclaredValue > 1000) riskScore += 1;

        // International shipments increase risk
        if (shipment.OriginCountryCode != shipment.DestinationCountryCode)
        {
            riskScore += 2;
        }

        // Export required increases complexity
        if (shipment.ExportRequired)
        {
            riskScore += 1;
        }

        // Heavy packages may have handling risks
        if (shipment.PackageWeight.HasValue && shipment.PackageWeight.Value > 5)
        {
            riskScore += 1;
        }

        // Special handling requirements
        if (!string.IsNullOrEmpty(shipment.SpecialHandlingNotes))
        {
            riskScore += 1;
        }

        // Map score to risk level
        return riskScore switch
        {
            >= 6 => "Critical",
            >= 4 => "High",
            >= 2 => "Medium",
            _ => "Low"
        };
    }

    private string GenerateRiskReasons(Shipment shipment, Order order, string riskLevel)
    {
        var reasons = new List<string>();

        if (shipment.DeclaredValue > 5000)
        {
            reasons.Add($"High declared value: {shipment.DeclaredValue:C}");
        }

        if (shipment.OriginCountryCode != shipment.DestinationCountryCode)
        {
            reasons.Add($"International shipment: {shipment.OriginCountryCode} -> {shipment.DestinationCountryCode}");
        }

        if (shipment.ExportRequired)
        {
            reasons.Add("Export documentation required");
        }

        if (shipment.PackageWeight.HasValue && shipment.PackageWeight.Value > 5)
        {
            reasons.Add($"Heavy package: {shipment.PackageWeight.Value} kg");
        }

        if (!string.IsNullOrEmpty(shipment.SpecialHandlingNotes))
        {
            reasons.Add("Special handling requirements noted");
        }

        return reasons.Count > 0 ? string.Join("; ", reasons) : "Standard domestic shipment with low risk factors";
    }

    private string DetermineServiceType(Shipment shipment, string riskLevel)
    {
        if (riskLevel == "Critical" || riskLevel == "High")
        {
            return "Express Insured";
        }
        else if (shipment.ExportRequired)
        {
            return "International Priority";
        }
        else if (shipment.DeclaredValue > 2000)
        {
            return "Priority Insured";
        }

        return "Standard";
    }

    private bool ShouldRecommendInsurance(decimal declaredValue, string riskLevel)
    {
        return declaredValue > 1000 || riskLevel == "High" || riskLevel == "Critical";
    }

    private decimal? CalculateRecommendedCoverage(decimal declaredValue, string riskLevel)
    {
        if (riskLevel == "Critical")
        {
            return declaredValue * 1.2m; // 120% coverage
        }
        else if (riskLevel == "High")
        {
            return declaredValue * 1.1m; // 110% coverage
        }
        else if (declaredValue > 1000)
        {
            return declaredValue; // 100% coverage
        }

        return null;
    }

    private string GenerateHandlingRequirements(string riskLevel, bool exportRequired)
    {
        var requirements = new List<string>();

        if (riskLevel == "Critical" || riskLevel == "High")
        {
            requirements.Add("Signature required on delivery");
            requirements.Add("Secure packaging verification");
            requirements.Add("Tamper-evident seals");
        }

        if (exportRequired)
        {
            requirements.Add("Customs documentation attached");
            requirements.Add("Export license verification");
        }

        if (riskLevel == "Critical")
        {
            requirements.Add("Armored transport recommended");
            requirements.Add("GPS tracking enabled");
        }

        return requirements.Count > 0 ? string.Join("; ", requirements) : "Standard handling procedures";
    }

    private string GenerateRequiredDocuments(bool exportRequired, Order order)
    {
        var documents = new List<string>
        {
            "Commercial invoice",
            "Packing list"
        };

        if (exportRequired)
        {
            documents.Add("Export declaration");
            documents.Add("Certificate of origin");
            documents.Add("Import permit (if required)");
        }

        if (order.GemListingId.HasValue)
        {
            documents.Add("Gemstone certificate");
        }

        return string.Join("; ", documents);
    }

    private string? GenerateWarnings(Shipment shipment, string riskLevel)
    {
        var warnings = new List<string>();

        if (shipment.OriginCountryCode != shipment.DestinationCountryCode)
        {
            warnings.Add("International shipping delays possible due to customs clearance");
        }

        if (riskLevel == "Critical")
        {
            warnings.Add("High-value shipment - enhanced security measures in place");
        }

        if (shipment.ExportRequired)
        {
            warnings.Add("Export processing may add 2-5 business days to delivery time");
        }

        return warnings.Count > 0 ? string.Join("; ", warnings) : null;
    }

    private static ShippingPlanResponseDto MapToResponseDto(ShippingPlan plan)
    {
        return new ShippingPlanResponseDto
        {
            Id = plan.Id,
            ShipmentId = plan.ShipmentId,
            RiskLevel = plan.RiskLevel,
            RiskReasons = plan.RiskReasons,
            RecommendedServiceType = plan.RecommendedServiceType,
            InsuranceRecommended = plan.InsuranceRecommended,
            RecommendedCoverageAmount = plan.RecommendedCoverageAmount,
            HandlingRequirements = plan.HandlingRequirements,
            RequiredDocuments = plan.RequiredDocuments,
            Warnings = plan.Warnings,
            IsApproved = plan.IsApproved,
            CreatedAt = plan.CreatedAt
        };
    }
}
