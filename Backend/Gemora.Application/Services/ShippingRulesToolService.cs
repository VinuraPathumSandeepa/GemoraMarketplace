using Gemora.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

/// <summary>
/// Implementation of IShippingRulesToolService that retrieves approved shipping services and rules.
/// These are allow-listed tool functions used by the Logistics & Courier Tool Agent.
/// 
/// In production, these would be loaded from configuration or a database.
/// </summary>
public class ShippingRulesToolService : IShippingRulesToolService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ShippingRulesToolService> _logger;

    public ShippingRulesToolService(
        IConfiguration configuration,
        ILogger<ShippingRulesToolService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<List<string>> GetApprovedShippingServicesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool: Retrieving approved shipping services");

        // Load from configuration section "Shipping:ApprovedServices"
        // Format: array of service names
        var services = _configuration.GetSection("Shipping:ApprovedServices").Get<List<string>>() 
                       ?? new List<string>();

        if (!services.Any())
        {
            // Fallback to hardcoded defaults for development/testing
            services = new List<string>
            {
                "Standard Ground",
                "Express Air",
                "Premium Overnight"
            };
            
            _logger.LogWarning("No approved services configured - using defaults");
        }

        await Task.CompletedTask;
        return services;
    }

    public async Task<List<string>> GetApprovedShippingRulesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Tool: Retrieving approved shipping rules");

        // Load from configuration section "Shipping:Rules"
        // Format: array of rule descriptions
        var rules = _configuration.GetSection("Shipping:Rules").Get<List<string>>() 
                    ?? new List<string>();

        if (!rules.Any())
        {
            // Fallback to hardcoded defaults for development/testing
            rules = new List<string>
            {
                "All shipments must have valid origin and destination",
                "Declared value must be greater than zero",
                "High-value shipments (>10000) require insurance",
                "International shipments require export documentation",
                "Gemstones over 10 carats require specialized packaging"
            };
            
            _logger.LogWarning("No shipping rules configured - using defaults");
        }

        await Task.CompletedTask;
        return rules;
    }
}
