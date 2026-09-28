using Gemora.Application.DTOs;

namespace Gemora.Application.Interfaces;

/// <summary>
/// Service for orchestrating the four specialized AI agents for shipping planning.
/// Uses Gemini AI integration for generating structured shipping plans.
/// </summary>
public interface IShippingAgentService
{
    /// <summary>
    /// Generates a validated shipping plan by coordinating four specialized AI agents.
    /// The plan will be persisted with PendingAdminApproval status.
    /// </summary>
    Task<ShippingPlanGenerationResult> GenerateShippingPlanAsync(
        Guid shipmentId,
        CancellationToken cancellationToken = default);
}

// Tool function interfaces for the Logistics & Courier Tool Agent
public interface IOrderToolService
{
    Task<OrderInfo?> ReadOrderAsync(Guid orderId, CancellationToken cancellationToken = default);
}

public interface IShippingRulesToolService
{
    Task<List<string>> GetApprovedShippingServicesAsync(CancellationToken cancellationToken = default);
    Task<List<string>> GetApprovedShippingRulesAsync(CancellationToken cancellationToken = default);
}

// Supporting models
public class OrderInfo
{
    public Guid Id { get; set; }
    public Guid BuyerUserId { get; set; }
    public Guid SellerUserId { get; set; }
    public string? GemType { get; set; }
    public decimal? WeightInCarats { get; set; }
    public decimal DeclaredValue { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public bool RequiresExport { get; set; }
    public string Status { get; set; } = string.Empty;
}
