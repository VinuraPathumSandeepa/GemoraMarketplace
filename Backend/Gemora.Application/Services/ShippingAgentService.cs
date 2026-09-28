using System.Text.Json;
using Gemora.Application.DTOs;
using Gemora.Application.Interfaces;
using Gemora.Domain.Entities;
using Gemora.Domain.Enums;
using Gemora.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

/// <summary>
/// Orchestrates four specialized AI agents for shipping plan generation.
/// 
/// Agent 1: Shipping Planner - Generates structured shipping plans
/// Agent 2: Gem Risk Analysis - Assesses gemstone shipping risks
/// Agent 3: Logistics & Courier Tool Agent - Retrieves operational facts via allow-listed tools
/// Agent 4: Compliance & Safety Agent - Validates plans against deterministic business rules
/// 
/// NOTE: This implementation uses simulated AI responses since Google.GenAI is not yet configured.
/// The structure supports real AI integration when credentials are provided.
/// </summary>
public class ShippingAgentService : IShippingAgentService
{
    private readonly IShipmentRepository _shipmentRepository;
    private readonly IShippingPlanRepository _shippingPlanRepository;
    private readonly IOrderToolService _orderToolService;
    private readonly IShippingRulesToolService _shippingRulesToolService;
    private readonly ILogger<ShippingAgentService> _logger;

    public ShippingAgentService(
        IShipmentRepository shipmentRepository,
        IShippingPlanRepository shippingPlanRepository,
        IOrderToolService orderToolService,
        IShippingRulesToolService shippingRulesToolService,
        ILogger<ShippingAgentService> logger)
    {
        _shipmentRepository = shipmentRepository;
        _shippingPlanRepository = shippingPlanRepository;
        _orderToolService = orderToolService;
        _shippingRulesToolService = shippingRulesToolService;
        _logger = logger;
    }

    public async Task<ShippingPlanGenerationResult> GenerateShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Step 1: Load trusted order and shipment context from backend
            var shipment = await _shipmentRepository.GetByIdAsync(shipmentId, cancellationToken);
            if (shipment == null)
            {
                return new ShippingPlanGenerationResult
                {
                    Success = false,
                    ErrorMessage = "Shipment not found"
                };
            }

            var orderInfo = await _orderToolService.ReadOrderAsync(shipment.OrderId, cancellationToken);
            if (orderInfo == null)
            {
                // The order module is not part of this solution yet. Use the trusted
                // shipment snapshot so the academic planning workflow remains usable;
                // a production integration should replace this fallback with a real
                // paid-order lookup and eligibility check.
                orderInfo = new OrderInfo
                {
                    Id = shipment.OrderId,
                    BuyerUserId = shipment.BuyerUserId,
                    SellerUserId = shipment.SellerUserId,
                    DeclaredValue = shipment.DeclaredValue,
                    Currency = shipment.Currency,
                    Origin = shipment.Origin,
                    Destination = shipment.Destination,
                    RequiresExport = !string.Equals(shipment.Origin, shipment.Destination, StringComparison.OrdinalIgnoreCase)
                };
            }

            // Step 2: Validate required input
            var validationErrors = ValidateInput(shipment, orderInfo);
            if (validationErrors.Any())
            {
                return new ShippingPlanGenerationResult
                {
                    Success = false,
                    ValidationErrors = validationErrors
                };
            }

            // Step 3: Retrieve approved shipping services and rules
            var approvedServices = await _shippingRulesToolService.GetApprovedShippingServicesAsync(cancellationToken);
            var approvedRules = await _shippingRulesToolService.GetApprovedShippingRulesAsync(cancellationToken);

            if (!approvedServices.Any())
            {
                return new ShippingPlanGenerationResult
                {
                    Success = false,
                    ErrorMessage = "No approved shipping services available"
                };
            }

            // Step 4: Agent 2 - Assess gemstone and route risk
            var riskAssessment = await AssessGemRiskAsync(orderInfo, shipment, approvedServices, approvedRules, cancellationToken);

            // Step 5: Agent 1 & 3 - Generate preliminary shipping plan using logistics tools
            var preliminaryPlan = await GeneratePreliminaryPlanAsync(
                shipment, orderInfo, approvedServices, approvedRules, riskAssessment, cancellationToken);

            // Step 6: Agent 4 - Validate the plan with Compliance & Safety Agent
            var complianceResult = await ValidateComplianceAsync(
                preliminaryPlan, shipment, orderInfo, approvedServices, approvedRules, riskAssessment, cancellationToken);

            if (!complianceResult.IsValid)
            {
                return new ShippingPlanGenerationResult
                {
                    Success = false,
                    ValidationErrors = complianceResult.ValidationErrors
                };
            }

            // Step 7: Build final validated plan
            var finalPlan = BuildShippingPlanDto(
                shipment, preliminaryPlan, riskAssessment, complianceResult);

            // Step 8: Persist the generated plan with PendingAdminApproval status
            var shippingPlanEntity = new ShippingPlan
            {
                ShipmentId = shipmentId,
                RiskLevel = riskAssessment.RiskLevel,
                RecommendedServiceType = finalPlan.RecommendedServiceType,
                InsuranceRecommended = finalPlan.InsuranceRecommended,
                RecommendedCoverage = finalPlan.RecommendedCoverage,
                Requirements = JsonSerializer.Serialize(finalPlan.Requirements),
                Warnings = JsonSerializer.Serialize(finalPlan.Warnings),
                GeneratedAt = DateTime.UtcNow,
                Status = "PendingAdminApproval"
            };

            await _shippingPlanRepository.AddAsync(shippingPlanEntity, cancellationToken);

            _logger.LogInformation(
                "Successfully generated shipping plan {PlanId} for shipment {ShipmentId} with status {Status}",
                shippingPlanEntity.Id, shipmentId, shippingPlanEntity.Status);

            return new ShippingPlanGenerationResult
            {
                Success = true,
                Plan = finalPlan
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Shipping plan generation cancelled for shipment {ShipmentId}", shipmentId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate shipping plan for shipment {ShipmentId}", shipmentId);
            return new ShippingPlanGenerationResult
            {
                Success = false,
                ErrorMessage = $"Plan generation failed: {ex.Message}"
            };
        }
    }

    #region Agent 1: Shipping Planner Agent

    /// <summary>
    /// Agent 1: Shipping Planner Agent
    /// Generates a structured shipping plan from validated order and shipment context.
    /// Uses logistics tools to retrieve operational facts.
    /// Does NOT execute the plan - only generates recommendations.
    /// </summary>
    private async Task<PreliminaryPlan> GeneratePreliminaryPlanAsync(
        Shipment shipment,
        OrderInfo orderInfo,
        List<string> approvedServices,
        List<string> approvedRules,
        RiskAssessmentResult riskAssessment,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent 1: Generating preliminary shipping plan");

        // In a real implementation, this would call Gemini AI with the following context:
        // - Order details (gem type, weight, value, origin, destination)
        // - Approved services and rules from tool functions
        // - Risk assessment results
        
        // For now, use deterministic logic based on available data
        var recommendedService = SelectBestService(approvedServices, riskAssessment.RiskLevel, shipment.DeclaredValue);
        
        var requirements = new List<string>();
        var warnings = new List<string>();

        // Add requirements based on risk level and value
        if (riskAssessment.RiskLevel == RiskLevel.High)
        {
            requirements.Add("Signature required upon delivery");
            requirements.Add("Insurance coverage mandatory");
            requirements.Add("Secure packaging verification");
            warnings.Add("High-value shipment - enhanced security protocols apply");
        }

        if (orderInfo.RequiresExport)
        {
            requirements.Add("Export documentation required");
            requirements.Add("Customs declaration form");
            warnings.Add("International shipment - customs clearance may cause delays");
        }

        if (shipment.DeclaredValue > 10000)
        {
            requirements.Add("Additional insurance documentation");
            warnings.Add($"High declared value: {shipment.DeclaredValue} {shipment.Currency}");
        }

        // Calculate recommended coverage (typically 110% of declared value for comprehensive coverage)
        var recommendedCoverage = Math.Round(shipment.DeclaredValue * 1.1m, 2);

        return new PreliminaryPlan
        {
            RecommendedServiceType = recommendedService,
            InsuranceRecommended = riskAssessment.RiskLevel != RiskLevel.Low || shipment.DeclaredValue > 5000,
            RecommendedCoverage = recommendedCoverage,
            Requirements = requirements,
            Warnings = warnings
        };
    }

    #endregion

    #region Agent 2: Gem Risk Analysis Agent

    /// <summary>
    /// Agent 2: Gem Risk Analysis Agent
    /// Assesses shipping risks associated with valuable gemstones.
    /// Evaluates weight, value, route complexity, and handling requirements.
    /// </summary>
    private async Task<RiskAssessmentResult> AssessGemRiskAsync(
        OrderInfo orderInfo,
        Shipment shipment,
        List<string> approvedServices,
        List<string> approvedRules,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent 2: Assessing gem risk for shipment");

        var riskReasons = new List<string>();
        var handlingWarnings = new List<string>();
        var precautions = new List<string>();

        // Determine base risk level
        var riskScore = 0;

        // Factor 1: Declared value
        if (shipment.DeclaredValue > 50000)
        {
            riskScore += 3;
            riskReasons.Add($"Very high declared value: {shipment.DeclaredValue} {shipment.Currency}");
            precautions.Add("Use armored transport service");
        }
        else if (shipment.DeclaredValue > 10000)
        {
            riskScore += 2;
            riskReasons.Add($"High declared value: {shipment.DeclaredValue} {shipment.Currency}");
        }
        else if (shipment.DeclaredValue > 1000)
        {
            riskScore += 1;
        }

        // Factor 2: Gem weight (if available)
        if (orderInfo.WeightInCarats.HasValue)
        {
            if (orderInfo.WeightInCarats.Value > 10)
            {
                riskScore += 2;
                riskReasons.Add($"Large gemstone: {orderInfo.WeightInCarats.Value} carats");
                handlingWarnings.Add("Handle with extreme care - large stone");
                precautions.Add("Use specialized gemstone packaging");
            }
            else if (orderInfo.WeightInCarats.Value > 5)
            {
                riskScore += 1;
                riskReasons.Add($"Medium-large gemstone: {orderInfo.WeightInCarats.Value} carats");
            }
        }

        // Factor 3: Destination complexity (export/international)
        if (orderInfo.RequiresExport)
        {
            riskScore += 2;
            riskReasons.Add("International export shipment");
            handlingWarnings.Add("Customs clearance required");
            precautions.Add("Ensure all export documentation is complete");
        }

        // Factor 4: Route distance (simple heuristic based on origin/destination)
        if (shipment.Origin != shipment.Destination)
        {
            riskScore += 1;
            riskReasons.Add($"Domestic route: {shipment.Origin} to {shipment.Destination}");
        }

        // Factor 5: Service availability
        if (approvedServices.Count < 2)
        {
            riskScore += 1;
            riskReasons.Add("Limited approved service options");
            handlingWarnings.Add("Fewer backup options available");
        }

        // Determine final risk level
        RiskLevel riskLevel;
        if (riskScore >= 5)
        {
            riskLevel = RiskLevel.High;
            handlingWarnings.Add("HIGH RISK SHIPMENT - Enhanced security and tracking required");
        }
        else if (riskScore >= 3)
        {
            riskLevel = RiskLevel.Medium;
            handlingWarnings.Add("MEDIUM RISK SHIPMENT - Standard enhanced precautions apply");
        }
        else
        {
            riskLevel = RiskLevel.Low;
        }

        return new RiskAssessmentResult
        {
            RiskLevel = riskLevel,
            RiskReasons = riskReasons,
            HandlingWarnings = handlingWarnings,
            RecommendedPrecautions = precautions
        };
    }

    #endregion

    #region Agent 3: Logistics & Courier Tool Agent

    // This agent's functionality is implemented through the injected tool services:
    // - IOrderToolService.ReadOrderAsync()
    // - IShippingRulesToolService.GetApprovedShippingServicesAsync()
    // - IShippingRulesToolService.GetApprovedShippingRulesAsync()
    // 
    // These tools provide trusted backend data without allowing arbitrary operations.

    #endregion

    #region Agent 4: Compliance & Safety Agent

    /// <summary>
    /// Agent 4: Compliance & Safety Agent
    /// Independently validates the proposed shipping plan against deterministic business rules.
    /// Uses ordinary C# validation - does NOT rely solely on LLM for compliance.
    /// Rejects or flags invalid plans instead of silently correcting critical values.
    /// </summary>
    private async Task<ComplianceValidationResult> ValidateComplianceAsync(
        PreliminaryPlan plan,
        Shipment shipment,
        OrderInfo orderInfo,
        List<string> approvedServices,
        List<string> approvedRules,
        RiskAssessmentResult riskAssessment,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Agent 4: Validating compliance for shipping plan");

        var validationErrors = new List<string>();

        // Rule 1: Validate referenced shipment ID
        if (shipment.Id == Guid.Empty)
        {
            validationErrors.Add("Invalid shipment ID");
        }

        // Rule 2: Validate service ID is in approved list
        if (!approvedServices.Contains(plan.RecommendedServiceType))
        {
            validationErrors.Add($"Service '{plan.RecommendedServiceType}' is not in the approved services list");
        }

        // Rule 3: Validate origin/destination are not empty
        if (string.IsNullOrWhiteSpace(shipment.Origin))
        {
            validationErrors.Add("Origin is required");
        }

        if (string.IsNullOrWhiteSpace(shipment.Destination))
        {
            validationErrors.Add("Destination is required");
        }

        // Rule 4: Validate risk level enum value
        if (!Enum.IsDefined(typeof(RiskLevel), riskAssessment.RiskLevel))
        {
            validationErrors.Add($"Invalid risk level: {riskAssessment.RiskLevel}");
        }

        // Rule 5: Validate declared value and currency
        if (shipment.DeclaredValue <= 0)
        {
            validationErrors.Add("Declared value must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(shipment.Currency))
        {
            validationErrors.Add("Currency is required");
        }

        // Rule 6: Validate insurance coverage limits
        if (plan.InsuranceRecommended && plan.RecommendedCoverage <= 0)
        {
            validationErrors.Add("Recommended insurance coverage must be greater than zero when insurance is recommended");
        }

        if (plan.RecommendedCoverage < shipment.DeclaredValue)
        {
            validationErrors.Add("Insurance coverage should not be less than declared value");
        }

        // Rule 7: Check for missing or inconsistent information
        if (!plan.Requirements.Any())
        {
            validationErrors.Add("At least one requirement should be specified");
        }

        // Rule 8: Validate package description is not empty
        if (string.IsNullOrWhiteSpace(shipment.PackageDescription))
        {
            validationErrors.Add("Package description is required");
        }

        // Rule 9: Check for prompt injection in package description (basic check)
        if (ContainsSuspiciousContent(shipment.PackageDescription))
        {
            validationErrors.Add("Package description contains suspicious content that may indicate prompt injection");
        }

        return new ComplianceValidationResult
        {
            IsValid = !validationErrors.Any(),
            ValidationErrors = validationErrors
        };
    }

    #endregion

    #region Helper Methods

    private static List<string> ValidateInput(Shipment shipment, OrderInfo orderInfo)
    {
        var errors = new List<string>();

        if (shipment.DeclaredValue <= 0)
        {
            errors.Add("Declared value must be greater than zero");
        }

        if (string.IsNullOrWhiteSpace(shipment.Currency))
        {
            errors.Add("Currency is required");
        }

        if (string.IsNullOrWhiteSpace(shipment.Origin))
        {
            errors.Add("Origin is required");
        }

        if (string.IsNullOrWhiteSpace(shipment.Destination))
        {
            errors.Add("Destination is required");
        }

        if (string.IsNullOrWhiteSpace(shipment.PackageDescription))
        {
            errors.Add("Package description is required");
        }

        if (string.IsNullOrWhiteSpace(shipment.SelectedService))
        {
            errors.Add("Selected service is required");
        }

        return errors;
    }

    private static string SelectBestService(List<string> approvedServices, RiskLevel riskLevel, decimal declaredValue)
    {
        // Simple selection logic - in production this would use AI or more sophisticated rules
        if (riskLevel == RiskLevel.High || declaredValue > 20000)
        {
            // Prefer premium/express services for high-risk shipments
            var premiumService = approvedServices.FirstOrDefault(s => 
                s.Contains("Premium", StringComparison.OrdinalIgnoreCase) || 
                s.Contains("Express", StringComparison.OrdinalIgnoreCase));
            
            return premiumService ?? approvedServices.First();
        }

        // Default to first approved service
        return approvedServices.First();
    }

    private static ShippingPlanDto BuildShippingPlanDto(
        Shipment shipment,
        PreliminaryPlan plan,
        RiskAssessmentResult riskAssessment,
        ComplianceValidationResult compliance)
    {
        return new ShippingPlanDto
        {
            ShipmentId = shipment.Id,
            RiskLevel = riskAssessment.RiskLevel,
            RecommendedServiceType = plan.RecommendedServiceType,
            InsuranceRecommended = plan.InsuranceRecommended,
            RecommendedCoverage = plan.RecommendedCoverage,
            Requirements = plan.Requirements,
            Warnings = plan.Warnings.Concat(riskAssessment.HandlingWarnings).ToList(),
            RiskReasons = riskAssessment.RiskReasons,
            GeneratedAt = DateTime.UtcNow,
            ApprovalStatus = "PendingAdminApproval"
        };
    }

    private static bool ContainsSuspiciousContent(string text)
    {
        // Basic prompt injection detection - look for common patterns
        var suspiciousPatterns = new[]
        {
            "ignore previous",
            "system prompt",
            "override instructions",
            "bypass security",
            "admin access",
            "execute command",
            "<script>",
            "javascript:",
            "DROP TABLE",
            "DELETE FROM"
        };

        var lowerText = text.ToLowerInvariant();
        return suspiciousPatterns.Any(pattern => lowerText.Contains(pattern));
    }

    #endregion

    #region Internal Models

    private class PreliminaryPlan
    {
        public string RecommendedServiceType { get; set; } = string.Empty;
        public bool InsuranceRecommended { get; set; }
        public decimal RecommendedCoverage { get; set; }
        public List<string> Requirements { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    private class ComplianceValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    #endregion
}
