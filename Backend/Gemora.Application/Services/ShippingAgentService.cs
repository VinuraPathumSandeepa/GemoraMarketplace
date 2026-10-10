using Gemora.Application.DTOs;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

public interface IShippingAgentService
{
    Task<ShippingPlanResponseDto> GenerateShippingPlanAsync(Guid shipmentId);
    Task<ShippingPlanResponseDto?> GetShippingPlanAsync(Guid shipmentId);
    Task<bool> ApproveShippingPlanAsync(Guid shipmentId, Guid adminId, string? adminNotes);
    Task<bool> RejectShippingPlanAsync(Guid shipmentId, Guid adminId, string? rejectionReason);
    Task<bool> RequestRevisionShippingPlanAsync(Guid shipmentId, Guid adminId, string? revisionNotes);
}

public class ShippingAgentService : IShippingAgentService
{
    private readonly ApplicationDbContext _context;
    private readonly ILlmProvider? _llmProvider;
    private readonly ILogger<ShippingAgentService>? _logger;

    public ShippingAgentService(
        ApplicationDbContext context,
        ILlmProvider? llmProvider = null,
        ILogger<ShippingAgentService>? logger = null)
    {
        _context = context;
        _llmProvider = llmProvider;
        _logger = logger;
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

        var analysisOnly = !string.IsNullOrWhiteSpace(shipment.TrackingNumber) ||
            shipment.Status is "Booked" or "PickedUp" or "InTransit" or "OutForDelivery" or "Delivered" or "Cancelled";
        if (order.TotalAmount <= 0 || shipment.DeclaredValue != order.TotalAmount || shipment.Currency != order.Currency)
            throw new InvalidOperationException("Shipment value and currency must match the paid order before risk analysis.");

        // Phase 7: Attempt AI-powered planning first, fall back to deterministic rules
        LlmShippingPlanRecommendation? aiRecommendation = null;
        bool usedFallback = false;
        string fallbackReason = "Provider unavailable";

        if (_llmProvider != null)
        {
            try
            {
                var llmInput = BuildLlmInput(shipment, order);
                aiRecommendation = await _llmProvider.GenerateShippingPlanAsync(llmInput);

                if (aiRecommendation != null)
                {
                    // Validate AI output before using it
                    var validationErrors = ValidateAiRecommendation(aiRecommendation, shipment);
                    if (validationErrors.Any())
                    {
                        _logger?.LogWarning("AI recommendation failed validation: {Errors}", string.Join("; ", validationErrors));
                        aiRecommendation = null;
                        usedFallback = true;
                        fallbackReason = "Model output failed backend validation";
                    }
                }
                else
                {
                    _logger?.LogInformation("LLM provider returned null, using fallback");
                    usedFallback = true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "LLM provider exception, falling back to deterministic rules");
                usedFallback = true;
                fallbackReason = ex is OperationCanceledException ? "Model request timed out" :
                    ex is InvalidOperationException ? ex.Message : "Provider response could not be processed";
            }
        }
        else
        {
            _logger?.LogInformation("No LLM provider configured, using deterministic fallback");
            usedFallback = true;
        }

        // Use AI recommendation or fall back to deterministic rules
        string riskLevel;
        string riskReasons;
        string recommendedService;
        bool insuranceRecommended;
        decimal? coverageAmount;
        string handlingRequirements;
        string requiredDocuments;
        string? warnings;
        string generationSource;
        string? executionSummary;

        if (aiRecommendation != null)
        {
            // Use validated AI recommendation
            riskLevel = aiRecommendation.RiskLevel;
            riskReasons = string.Join("; ", aiRecommendation.RiskReasons);
            recommendedService = aiRecommendation.RecommendedServiceType;
            insuranceRecommended = aiRecommendation.InsuranceRecommended;
            coverageAmount = aiRecommendation.RecommendedCoverageAmount;
            handlingRequirements = string.Join("; ", aiRecommendation.HandlingRequirements);
            requiredDocuments = string.Join("; ", aiRecommendation.RequiredDocuments);
            warnings = aiRecommendation.Warnings.Any() ? string.Join("; ", aiRecommendation.Warnings) : null;
            generationSource = _llmProvider?.GetType().Name == "MockLlmProvider" ? "MockProvider" : "AI";
            executionSummary = aiRecommendation.ExecutionSummary;
        }
        else
        {
            // Fall back to deterministic rules
            riskLevel = AssessRiskLevel(shipment, order);
            riskReasons = GenerateRiskReasons(shipment, order, riskLevel);
            recommendedService = DetermineServiceType(shipment, riskLevel);
            insuranceRecommended = ShouldRecommendInsurance(shipment.DeclaredValue, riskLevel);
            coverageAmount = CalculateRecommendedCoverage(shipment.DeclaredValue, riskLevel);
            handlingRequirements = GenerateHandlingRequirements(riskLevel, shipment.ExportRequired);
            requiredDocuments = GenerateRequiredDocuments(shipment.ExportRequired, order);
            warnings = GenerateWarnings(shipment, riskLevel);
            generationSource = "FallbackRules";
            executionSummary = $"Rules fallback; real AI analysis did not complete. Reason: {fallbackReason}. risk={riskLevel}, service={recommendedService}";
        }

        // Check if plan already exists
        var existingPlan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        // Existing fulfilment must remain intact; return a fresh assessment without saving.
        if (analysisOnly)
        {
            return new ShippingPlanResponseDto
            {
                IsPreview = true,
                Id = existingPlan?.Id ?? Guid.NewGuid(),
                ShipmentId = shipmentId,
                RiskLevel = riskLevel,
                RiskReasons = riskReasons,
                RecommendedServiceType = recommendedService,
                InsuranceRecommended = insuranceRecommended,
                RecommendedCoverageAmount = coverageAmount,
                HandlingRequirements = handlingRequirements,
                RequiredDocuments = requiredDocuments,
                Warnings = warnings,
                GenerationSource = generationSource,
                ExecutionSummary = executionSummary,
                IsApproved = existingPlan?.IsApproved ?? false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        if (existingPlan != null)
        {
            // Update existing plan - REGENERATION INVALIDATES APPROVAL
            existingPlan.RiskLevel = riskLevel;
            existingPlan.RiskReasons = riskReasons;
            existingPlan.RecommendedServiceType = recommendedService;
            existingPlan.InsuranceRecommended = insuranceRecommended;
            existingPlan.RecommendedCoverageAmount = coverageAmount;
            existingPlan.HandlingRequirements = handlingRequirements;
            existingPlan.RequiredDocuments = requiredDocuments;
            existingPlan.Warnings = warnings;
            existingPlan.GenerationSource = generationSource;
            existingPlan.ExecutionSummary = executionSummary;

            // Invalidate previous approval - Admin must review new plan
            if (existingPlan.IsApproved)
            {
                existingPlan.IsApproved = false;
                existingPlan.ApprovedBy = null;
                existingPlan.ApprovedAt = null;
                existingPlan.AdminNotes = null;
            }

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
                GenerationSource = generationSource,
                ExecutionSummary = executionSummary,
                IsApproved = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.ShippingPlans.Add(plan);
        }

        // Update shipment risk level
        shipment.RiskLevel = riskLevel;
        if (shipment.Status is "Pending" or "Planning" or "PlanApproved" or "ReadyForBooking")
        {
            shipment.Status = "PlanGenerated";
        }
        shipment.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var savedPlan = existingPlan ?? await _context.ShippingPlans.FirstAsync(p => p.ShipmentId == shipmentId);

        _logger?.LogInformation("Shipping plan generated for {ShipmentId}: source={Source}, risk={Risk}, usedFallback={Fallback}",
            shipmentId, savedPlan.GenerationSource, riskLevel, usedFallback);

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

        // Validate shipment exists
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        if (shipment.Status is not ("Pending" or "Planning" or "PlanGenerated" or "ReadyForBooking"))
            throw new InvalidOperationException("Cannot approve a plan after courier dispatch has started.");

        // APPROVAL TRANSACTION: Update plan AND shipment status together
        plan.IsApproved = true;
        plan.ApprovedBy = adminId;
        plan.ApprovedAt = DateTime.UtcNow;
        plan.AdminNotes = adminNotes;
        plan.UpdatedAt = DateTime.UtcNow;

        var previousStatus = shipment.Status;
        // Set shipment to ReadyForBooking (required by booking service precondition)
        shipment.Status = "ReadyForBooking";
        shipment.UpdatedAt = DateTime.UtcNow;

        // Create audit tracking event for approval
        var approvalEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            EventType = "PlanApproved",
            Location = "Admin Review",
            Description = $"Shipping plan approved by Admin (ID: {adminId})",
            PerformedByUserId = adminId,
            PerformedByRole = "Admin",
            PreviousState = previousStatus,
            NewState = "ReadyForBooking",
            Reason = adminNotes,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(approvalEvent);

        await ShipmentOrderProgress.RecordApprovalAsync(_context, shipment, adminId);

        // Single SaveChanges ensures atomic transaction
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RejectShippingPlanAsync(Guid shipmentId, Guid adminId, string? rejectionReason)
    {
        var plan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (plan == null)
        {
            throw new InvalidOperationException("Shipping plan not found.");
        }

        if (plan.IsApproved)
        {
            throw new InvalidOperationException("Cannot reject an already approved shipping plan.");
        }

        // Validate shipment exists
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // REJECTION TRANSACTION: Update plan notes AND reset shipment status
        plan.IsApproved = false;
        plan.ApprovedBy = null;
        plan.ApprovedAt = null;
        plan.AdminNotes = rejectionReason ?? "Rejected by Admin";
        plan.UpdatedAt = DateTime.UtcNow;

        // Reset shipment back to Pending so seller can create a new plan
        shipment.Status = "Pending";
        shipment.UpdatedAt = DateTime.UtcNow;

        // Create audit tracking event for rejection
        var rejectionEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            EventType = "PlanRejected",
            Location = "Admin Review",
            Description = $"Shipping plan rejected by Admin (ID: {adminId}). Reason: {rejectionReason ?? "No reason provided"}",
            PerformedByUserId = adminId,
            PerformedByRole = "Admin",
            PreviousState = shipment.Status,
            NewState = "Pending",
            Reason = rejectionReason,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(rejectionEvent);

        // Single SaveChanges ensures atomic transaction
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RequestRevisionShippingPlanAsync(Guid shipmentId, Guid adminId, string? revisionNotes)
    {
        var plan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (plan == null)
        {
            throw new InvalidOperationException("Shipping plan not found.");
        }

        if (plan.IsApproved)
        {
            throw new InvalidOperationException("Cannot request revision for an already approved shipping plan.");
        }

        // Validate shipment exists
        var shipment = await _context.Shipments
            .FirstOrDefaultAsync(s => s.Id == shipmentId);

        if (shipment == null)
        {
            throw new InvalidOperationException("Shipment not found.");
        }

        // REVISION REQUEST TRANSACTION: Update plan notes AND set shipment to Planning state
        plan.IsApproved = false;
        plan.ApprovedBy = null;
        plan.ApprovedAt = null;
        plan.AdminNotes = revisionNotes ?? "Revision requested by Admin";
        plan.UpdatedAt = DateTime.UtcNow;

        // Set shipment to Planning so seller knows to regenerate the plan
        shipment.Status = "Planning";
        shipment.UpdatedAt = DateTime.UtcNow;

        // Create audit tracking event for revision request
        var revisionEvent = new ShipmentTrackingEvent
        {
            Id = Guid.NewGuid(),
            ShipmentId = shipmentId,
            EventType = "RevisionRequested",
            Location = "Admin Review",
            Description = $"Admin requested plan revision (ID: {adminId}). Notes: {revisionNotes ?? "No notes provided"}",
            PerformedByUserId = adminId,
            PerformedByRole = "Admin",
            PreviousState = shipment.Status,
            NewState = "Planning",
            Reason = revisionNotes,
            OccurredAt = DateTime.UtcNow,
            RecordedAt = DateTime.UtcNow
        };

        _context.ShipmentTrackingEvents.Add(revisionEvent);

        // Single SaveChanges ensures atomic transaction
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<ShippingPlanResponseDto?> GetShippingPlanAsync(Guid shipmentId)
    {
        var plan = await _context.ShippingPlans
            .FirstOrDefaultAsync(p => p.ShipmentId == shipmentId);

        if (plan == null)
        {
            return null;
        }

        return MapToResponseDto(plan);
    }

    // Phase 7: Build trusted backend data for LLM (never includes client-supplied values)
    private LlmShippingPlanInput BuildLlmInput(Shipment shipment, Order order)
    {
        var approvedServices = new List<string>
        {
            "Standard",
            "Express Insured",
            "Priority Insured",
            "International Priority"
        };

        return new LlmShippingPlanInput
        {
            ShipmentId = shipment.Id,
            DeclaredValue = shipment.DeclaredValue,
            Currency = shipment.Currency,
            OriginCountryCode = shipment.OriginCountryCode ?? "",
            DestinationCountryCode = shipment.DestinationCountryCode ?? "",
            ExportRequired = shipment.ExportRequired || shipment.OriginCountryCode != shipment.DestinationCountryCode,
            PackageWeight = shipment.PackageWeight,
            SpecialHandlingNotes = shipment.SpecialHandlingNotes,
            GemType = order.GemListing?.GemType,
            ListingTitle = order.GemListing?.Title,
            SellerRole = shipment.Seller?.Role ?? "Seller",
            BuyerRole = shipment.Buyer?.Role ?? "Buyer",
            ApprovedServiceTypes = approvedServices
        };
    }

    // Phase 7: Validate AI recommendation against strict schema requirements
    private List<string> ValidateAiRecommendation(LlmShippingPlanRecommendation recommendation, Shipment shipment)
    {
        var errors = new List<string>();

        // Validate risk level enum
        var validRiskLevels = new[] { "Low", "Medium", "High", "Critical" };
        if (!validRiskLevels.Contains(recommendation.RiskLevel))
        {
            errors.Add($"Invalid RiskLevel: {recommendation.RiskLevel}");
        }

        // Validate service type is non-empty
        var eligibleServices = BuildLlmInput(shipment, new Order()).ApprovedServiceTypes
            .Where(s => shipment.OriginCountryCode != shipment.DestinationCountryCode
                ? s == "International Priority" : s != "International Priority");
        if (!eligibleServices.Contains(recommendation.RecommendedServiceType))
        {
            errors.Add("RecommendedServiceType must be approved for this route");
        }

        // Validate coverage amount bounds
        if (recommendation.RecommendedCoverageAmount.HasValue)
        {
            if (recommendation.RecommendedCoverageAmount.Value < 0)
            {
                errors.Add("RecommendedCoverageAmount cannot be negative");
            }

            // Coverage should not exceed 200% of declared value
            if (recommendation.RecommendedCoverageAmount.Value > shipment.DeclaredValue * 2)
            {
                errors.Add($"RecommendedCoverageAmount exceeds 200% of declared value ({shipment.DeclaredValue})");
            }
        }

        // Validate required lists are not null (empty is OK)
        if (recommendation.RiskReasons == null)
        {
            errors.Add("RiskReasons list cannot be null");
        }

        if (recommendation.HandlingRequirements == null)
        {
            errors.Add("HandlingRequirements list cannot be null");
        }

        if (recommendation.RequiredDocuments == null)
        {
            errors.Add("RequiredDocuments list cannot be null");
        }

        if (recommendation.Warnings == null)
        {
            errors.Add("Warnings list cannot be null");
        }

        // Validate generation source metadata
        if (!string.IsNullOrEmpty(recommendation.GenerationSource) &&
            recommendation.GenerationSource != "AI" &&
            recommendation.GenerationSource != "FallbackRules")
        {
            errors.Add($"Invalid GenerationSource: {recommendation.GenerationSource}");
        }

        return errors;
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
            GenerationSource = plan.GenerationSource,
            ExecutionSummary = plan.ExecutionSummary,
            UpdatedAt = plan.UpdatedAt,
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
            ApprovedBy = plan.ApprovedBy,
            ApprovedAt = plan.ApprovedAt,
            AdminNotes = plan.AdminNotes,
            CreatedAt = plan.CreatedAt
        };
    }
}
