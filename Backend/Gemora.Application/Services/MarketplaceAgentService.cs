using System.Text.Json;

using Gemora.Application.Configuration;
using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.DTOs.MarketplaceAgent;
using Gemora.Application.Interfaces;

using Gemora.Domain.Constants;

using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

using LegacyRequest =
    Gemora.Application.DTOs.Marketplace.MarketplaceAgentRequestDto;

using LegacyResponse =
    Gemora.Application.DTOs.Marketplace.MarketplaceAgentResponseDto;

using ChatRequest =
    Gemora.Application.DTOs.MarketplaceAgent.MarketplaceAgentRequestDto;

using ChatResponse =
    Gemora.Application.DTOs.MarketplaceAgent.MarketplaceAgentResponseDto;


namespace Gemora.Application.Services;


public class MarketplaceAgentService
    : IMarketplaceAgentService
{
    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private readonly ApplicationDbContext _context;

    private readonly IMarketplaceAiClient _aiClient;

    private readonly string _modelName;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public MarketplaceAgentService(
        ApplicationDbContext context,
        IMarketplaceAiClient aiClient,
        IOptions<GeminiMarketplaceOptions> options)
    {
        _context =
            context;

        _aiClient =
            aiClient;

        _modelName =
            options.Value.Model;
    }


    // ============================================================
    // GEMINI TOOL-BASED MARKETPLACE AGENT
    // ============================================================

    public async Task<ChatResponse> AskAsync(
        Guid buyerId,
        ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateBuyerAsync(
            buyerId,
            cancellationToken
        );


        if (
            string.IsNullOrWhiteSpace(
                request.Message)
        )
        {
            throw new InvalidOperationException(
                "Please enter a message for the marketplace assistant."
            );
        }


        var message =
            request.Message.Trim();


        var conversationId =
            string.IsNullOrWhiteSpace(
                request.ConversationId)

                ? Guid.NewGuid()
                    .ToString("N")

                : request.ConversationId.Trim();


        var systemPrompt =
            BuildSystemPrompt();


        // ========================================================
        // STEP 1
        // ASK GEMINI WHETHER A BACKEND TOOL IS REQUIRED
        // ========================================================

        var decision =
            await _aiClient.GetDecisionAsync(
                message,
                systemPrompt,
                cancellationToken
            );


        // ========================================================
        // STEP 2
        // GENERAL QUESTION / NO TOOL REQUIRED
        // ========================================================

        if (
            !decision.RequiresTool
        )
        {
            return new ChatResponse
            {
                Message =
                    string.IsNullOrWhiteSpace(
                        decision.DirectResponse)

                        ? "I could not generate a response."

                        : decision.DirectResponse.Trim(),

                ConversationId =
                    conversationId,

                Model =
                    _modelName,

                ToolsUsed =
                    new List<string>(),

                GeneratedAt =
                    DateTime.UtcNow
            };
        }


        // ========================================================
        // STEP 3
        // EXECUTE SAFE GEMORA BACKEND TOOL
        // ========================================================

        var toolResult =
            await ExecuteToolAsync(
                buyerId,
                decision,
                cancellationToken
            );


        // ========================================================
        // STEP 4
        // GIVE VERIFIED BACKEND RESULT TO GEMINI
        // ========================================================

        var finalMessage =
            await _aiClient
                .GenerateFinalResponseAsync(
                    message,
                    systemPrompt,
                    toolResult,
                    cancellationToken
                );


        return new ChatResponse
        {
            Message =
                finalMessage,

            ConversationId =
                conversationId,

            Model =
                _modelName,

            ToolsUsed =
                string.IsNullOrWhiteSpace(
                    decision.ToolName)

                    ? new List<string>()

                    : new List<string>
                    {
                        decision.ToolName!
                    },

            GeneratedAt =
                DateTime.UtcNow
        };
    }


    // ============================================================
    // TOOL ROUTER
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        ExecuteToolAsync(
            Guid buyerId,
            MarketplaceAiDecisionDto decision,
            CancellationToken cancellationToken)
    {
        if (
            string.IsNullOrWhiteSpace(
                decision.ToolName)
        )
        {
            return FailedToolResult(
                "unknown",
                "Gemini did not provide a valid tool name."
            );
        }


        return decision.ToolName switch
        {
            MarketplaceAgentToolNames.SearchGems =>
                await SearchGemsToolAsync(
                    decision.ToolArguments,
                    cancellationToken
                ),

            MarketplaceAgentToolNames.GetGemDetails =>
                await GetGemDetailsToolAsync(
                    decision.ToolArguments,
                    cancellationToken
                ),

            MarketplaceAgentToolNames.CompareGems =>
                await CompareGemsToolAsync(
                    decision.ToolArguments,
                    cancellationToken
                ),

            MarketplaceAgentToolNames.GetMyOrders =>
                await GetMyOrdersToolAsync(
                    buyerId,
                    cancellationToken
                ),

            MarketplaceAgentToolNames.GetOrderDetails =>
                await GetOrderDetailsToolAsync(
                    buyerId,
                    decision.ToolArguments,
                    cancellationToken
                ),

            MarketplaceAgentToolNames.GetShipmentTracking =>
                await GetShipmentTrackingToolAsync(
                    buyerId,
                    decision.ToolArguments,
                    cancellationToken
                ),

            _ =>
                FailedToolResult(
                    decision.ToolName,
                    "The requested marketplace tool is not supported."
                )
        };
    }


    // ============================================================
    // TOOL 1
    // SEARCH APPROVED + CURRENTLY AVAILABLE GEMS
    //
    // IMPORTANT:
    //
    // Current Gemora database does NOT depend on:
    //
    // GemListing.AvailabilityStatus
    //
    // Availability is determined by checking whether a gemstone
    // already has an active order.
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        SearchGemsToolAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
    {
        var search =
            GetString(
                arguments,
                "search"
            );

        var gemType =
            GetString(
                arguments,
                "gemType"
            );

        var color =
            GetString(
                arguments,
                "color"
            );

        var sort =
            GetString(
                arguments,
                "sort"
            );

        var minPrice =
            GetDecimal(
                arguments,
                "minPrice"
            );

        var maxPrice =
            GetDecimal(
                arguments,
                "maxPrice"
            );

        var minCarat =
            GetDecimal(
                arguments,
                "minCarat"
            );

        var maxCarat =
            GetDecimal(
                arguments,
                "maxCarat"
            );


        // --------------------------------------------------------
        // VALIDATE PRICE RANGE
        // --------------------------------------------------------

        if (
            minPrice.HasValue &&
            minPrice.Value < 0
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Minimum price cannot be negative."
            );
        }


        if (
            maxPrice.HasValue &&
            maxPrice.Value < 0
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Maximum price cannot be negative."
            );
        }


        if (
            minPrice.HasValue &&
            maxPrice.HasValue &&
            minPrice.Value >
            maxPrice.Value
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Minimum price cannot be greater than maximum price."
            );
        }


        // --------------------------------------------------------
        // VALIDATE CARAT RANGE
        // --------------------------------------------------------

        if (
            minCarat.HasValue &&
            minCarat.Value < 0
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Minimum carat cannot be negative."
            );
        }


        if (
            maxCarat.HasValue &&
            maxCarat.Value < 0
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Maximum carat cannot be negative."
            );
        }


        if (
            minCarat.HasValue &&
            maxCarat.HasValue &&
            minCarat.Value >
            maxCarat.Value
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.SearchGems,
                "Minimum carat cannot be greater than maximum carat."
            );
        }


        // --------------------------------------------------------
        // BASE MARKETPLACE QUERY
        //
        // Approved listings only.
        //
        // No active order:
        // - Cancelled is ignored
        // - Refunded is ignored
        // - Failed is ignored
        //
        // Any other order status means the gemstone is currently
        // reserved / purchased / unavailable.
        // --------------------------------------------------------

        var query =
            _context.GemListings
                .AsNoTracking()
                .Where(g =>
                    g.Status ==
                        GemListingStatuses.Approved
                    &&
                    !g.Orders.Any(o =>
                        o.Status !=
                            OrderStatuses.Cancelled
                        &&
                        o.Status !=
                            OrderStatuses.Refunded
                        &&
                        o.Status !=
                            OrderStatuses.Failed
                    )
                );


        // --------------------------------------------------------
        // GENERAL SEARCH
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                search)
        )
        {
            var value =
                search
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.Title
                        .ToLower()
                        .Contains(value)
                    ||
                    g.GemType
                        .ToLower()
                        .Contains(value)
                    ||
                    g.Description
                        .ToLower()
                        .Contains(value)
                    ||
                    g.Color
                        .ToLower()
                        .Contains(value)
                    ||
                    g.Cut
                        .ToLower()
                        .Contains(value)
                    ||
                    g.Region
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // GEM TYPE
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                gemType)
        )
        {
            var value =
                gemType
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.GemType
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // COLOR
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                color)
        )
        {
            var value =
                color
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.Color
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // PRICE
        // --------------------------------------------------------

        if (
            minPrice.HasValue
        )
        {
            query =
                query.Where(g =>
                    g.Price >=
                    minPrice.Value
                );
        }


        if (
            maxPrice.HasValue
        )
        {
            query =
                query.Where(g =>
                    g.Price <=
                    maxPrice.Value
                );
        }


        // --------------------------------------------------------
        // CARAT
        // --------------------------------------------------------

        if (
            minCarat.HasValue
        )
        {
            query =
                query.Where(g =>
                    g.CaratWeight >=
                    minCarat.Value
                );
        }


        if (
            maxCarat.HasValue
        )
        {
            query =
                query.Where(g =>
                    g.CaratWeight <=
                    maxCarat.Value
                );
        }


        // --------------------------------------------------------
        // SORT
        // --------------------------------------------------------

        query =
            sort?
                .Trim()
                .ToLowerInvariant() switch
            {
                "price_low" =>
                    query.OrderBy(g =>
                        g.Price),

                "price_asc" =>
                    query.OrderBy(g =>
                        g.Price),

                "price_high" =>
                    query.OrderByDescending(g =>
                        g.Price),

                "price_desc" =>
                    query.OrderByDescending(g =>
                        g.Price),

                "carat_low" =>
                    query.OrderBy(g =>
                        g.CaratWeight),

                "carat_high" =>
                    query.OrderByDescending(g =>
                        g.CaratWeight),

                "carat_desc" =>
                    query.OrderByDescending(g =>
                        g.CaratWeight),

                _ =>
                    query.OrderByDescending(g =>
                        g.CreatedAt)
            };


        var gems =
            await query
                .Take(10)
                .Select(g =>
                    new
                    {
                        g.Id,

                        g.Title,

                        g.GemType,

                        g.Description,

                        g.CaratWeight,

                        g.Color,

                        g.Clarity,

                        g.Cut,

                        g.Price,

                        g.Currency,

                        g.CountryCode,

                        g.Region,

                        g.PrimaryImageUrl,

                        g.CertificateNumber,

                        g.CertificateAuthority,

                        SellerName =
                            g.Seller.FullName,

                        IsAvailable =
                            true
                    }
                )
                .ToListAsync(
                    cancellationToken
                );


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames.SearchGems,

            Success =
                true,

            Message =
                gems.Count == 0

                    ? "No approved and currently available gemstones matched the search."

                    : $"{gems.Count} approved and currently available gemstones matched the search.",

            Data =
                gems
        };
    }


    // ============================================================
    // TOOL 2
    // GET GEM DETAILS
    //
    // Any APPROVED gemstone can be viewed.
    //
    // This allows a buyer to view a purchased gemstone from
    // order history even after it disappears from marketplace
    // browsing.
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        GetGemDetailsToolAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
    {
        var gemListingId =
            GetInt(
                arguments,
                "gemListingId"
            );


        if (
            !gemListingId.HasValue ||
            gemListingId.Value <= 0
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetGemDetails,
                "A valid gemstone listing ID is required."
            );
        }


        var gem =
            await _context.GemListings
                .AsNoTracking()
                .Where(g =>
                    g.Id ==
                        gemListingId.Value
                    &&
                    g.Status ==
                        GemListingStatuses.Approved
                )
                .Select(g =>
                    new
                    {
                        g.Id,

                        g.Title,

                        g.GemType,

                        g.Description,

                        g.CaratWeight,

                        g.Color,

                        g.Clarity,

                        g.Cut,

                        g.Price,

                        g.Currency,

                        g.CountryCode,

                        g.Region,

                        g.PrimaryImageUrl,

                        g.CertificateNumber,

                        g.CertificateAuthority,

                        SellerName =
                            g.Seller.FullName,

                        IsAvailable =
                            !g.Orders.Any(o =>
                                o.Status !=
                                    OrderStatuses.Cancelled
                                &&
                                o.Status !=
                                    OrderStatuses.Refunded
                                &&
                                o.Status !=
                                    OrderStatuses.Failed
                            )
                    }
                )
                .FirstOrDefaultAsync(
                    cancellationToken
                );


        if (
            gem == null
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetGemDetails,
                "The approved gemstone listing was not found."
            );
        }


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames.GetGemDetails,

            Success =
                true,

            Message =
                "Gemstone details retrieved successfully.",

            Data =
                gem
        };
    }


    // ============================================================
    // TOOL 3
    // COMPARE AVAILABLE GEMS
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        CompareGemsToolAsync(
            JsonElement? arguments,
            CancellationToken cancellationToken)
    {
        var ids =
            GetIntArray(
                arguments,
                "gemListingIds"
            );


        ids =
            ids
                .Where(id =>
                    id > 0)
                .Distinct()
                .Take(5)
                .ToList();


        if (
            ids.Count < 2
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.CompareGems,
                "At least two valid gemstone listing IDs are required."
            );
        }


        var gems =
            await _context.GemListings
                .AsNoTracking()
                .Where(g =>
                    ids.Contains(
                        g.Id)
                    &&
                    g.Status ==
                        GemListingStatuses.Approved
                    &&
                    !g.Orders.Any(o =>
                        o.Status !=
                            OrderStatuses.Cancelled
                        &&
                        o.Status !=
                            OrderStatuses.Refunded
                        &&
                        o.Status !=
                            OrderStatuses.Failed
                    )
                )
                .Select(g =>
                    new
                    {
                        g.Id,

                        g.Title,

                        g.GemType,

                        g.Description,

                        g.CaratWeight,

                        g.Color,

                        g.Clarity,

                        g.Cut,

                        g.Price,

                        g.Currency,

                        g.CountryCode,

                        g.Region,

                        g.CertificateNumber,

                        g.CertificateAuthority,

                        g.PrimaryImageUrl,

                        SellerName =
                            g.Seller.FullName,

                        IsAvailable =
                            true
                    }
                )
                .ToListAsync(
                    cancellationToken
                );


        if (
            gems.Count < 2
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.CompareGems,
                "At least two selected gemstones must still be approved and currently available."
            );
        }


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames.CompareGems,

            Success =
                true,

            Message =
                $"{gems.Count} gemstones were retrieved for comparison.",

            Data =
                gems
        };
    }


    // ============================================================
    // TOOL 4
    // GET AUTHENTICATED BUYER'S RECENT ORDERS
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        GetMyOrdersToolAsync(
            Guid buyerId,
            CancellationToken cancellationToken)
    {
        var orders =
            await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.BuyerId ==
                        buyerId
                )
                .OrderByDescending(o =>
                    o.CreatedAt
                )
                .Take(10)
                .Select(o =>
                    new
                    {
                        o.Id,

                        o.GemListingId,

                        GemTitle =
                            o.GemListing != null

                                ? o.GemListing.Title

                                : string.Empty,

                        GemImageUrl =
                            o.GemListing != null

                                ? o.GemListing
                                    .PrimaryImageUrl

                                : null,

                        Amount =
                            o.TotalAmount,

                        o.Currency,

                        o.Status,

                        o.FulfillmentStatus,

                        o.PaidAt,

                        o.HandedOverAt,

                        o.DeliveredAt,

                        o.CreatedAt,

                        Shipment =
                            o.Shipment == null

                                ? null

                                : new
                                {
                                    o.Shipment
                                        .CourierName,

                                    o.Shipment
                                        .TrackingNumber,

                                    o.Shipment
                                        .TrackingUrl,

                                    o.Shipment
                                        .ExpectedDeliveryDate,

                                    o.Shipment
                                        .DispatchNote,

                                    o.Shipment
                                        .Status,

                                    o.Shipment
                                        .HandedOverAt,

                                    o.Shipment
                                        .DeliveredAt
                                }
                    }
                )
                .ToListAsync(
                    cancellationToken
                );


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames.GetMyOrders,

            Success =
                true,

            Message =
                orders.Count == 0

                    ? "The authenticated buyer does not have any orders yet."

                    : $"{orders.Count} recent buyer orders were retrieved.",

            Data =
                orders
        };
    }


    // ============================================================
    // TOOL 5
    // GET AUTHENTICATED BUYER ORDER DETAILS
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        GetOrderDetailsToolAsync(
            Guid buyerId,
            JsonElement? arguments,
            CancellationToken cancellationToken)
    {
        var orderId =
            GetGuid(
                arguments,
                "orderId"
            );


        if (
            !orderId.HasValue
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetOrderDetails,
                "A valid order ID is required."
            );
        }


        // --------------------------------------------------------
        // SECURITY:
        //
        // Buyer ID comes from authenticated JWT.
        // Gemini cannot choose another Buyer ID.
        // --------------------------------------------------------

        var order =
            await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Id ==
                        orderId.Value
                    &&
                    o.BuyerId ==
                        buyerId
                )
                .Select(o =>
                    new
                    {
                        o.Id,

                        o.GemListingId,

                        GemTitle =
                            o.GemListing != null

                                ? o.GemListing.Title

                                : string.Empty,

                        GemType =
                            o.GemListing != null

                                ? o.GemListing.GemType

                                : string.Empty,

                        CaratWeight =
                            o.GemListing != null

                                ? o.GemListing.CaratWeight

                                : 0m,

                        GemColor =
                            o.GemListing != null

                                ? o.GemListing.Color

                                : string.Empty,

                        GemClarity =
                            o.GemListing != null

                                ? o.GemListing.Clarity

                                : string.Empty,

                        GemCut =
                            o.GemListing != null

                                ? o.GemListing.Cut

                                : string.Empty,

                        GemImageUrl =
                            o.GemListing != null

                                ? o.GemListing
                                    .PrimaryImageUrl

                                : null,

                        Amount =
                            o.TotalAmount,

                        o.Currency,

                        o.Status,

                        o.FulfillmentStatus,

                        o.CreatedAt,

                        o.UpdatedAt,

                        o.PaidAt,

                        o.HandedOverAt,

                        o.DeliveredAt,

                        DeliveryDetails =
                            o.DeliveryDetails == null

                                ? null

                                : new
                                {
                                    o.DeliveryDetails
                                        .RecipientName,

                                    o.DeliveryDetails
                                        .RecipientPhone,

                                    o.DeliveryDetails
                                        .AlternatePhone,

                                    o.DeliveryDetails
                                        .AddressLine1,

                                    o.DeliveryDetails
                                        .AddressLine2,

                                    o.DeliveryDetails
                                        .City,

                                    o.DeliveryDetails
                                        .District,

                                    o.DeliveryDetails
                                        .Region,

                                    o.DeliveryDetails
                                        .PostalCode,

                                    o.DeliveryDetails
                                        .CountryCode,

                                    o.DeliveryDetails
                                        .NearestLandmark,

                                    o.DeliveryDetails
                                        .DeliveryInstructions,

                                    o.DeliveryDetails
                                        .SignatureRequired,

                                    IsLocked =
                                        o.DeliveryDetails
                                            .LockedAt != null,

                                    o.DeliveryDetails
                                        .LockedAt
                                },

                        Shipment =
                            o.Shipment == null

                                ? null

                                : new
                                {
                                    o.Shipment
                                        .CourierName,

                                    o.Shipment
                                        .TrackingNumber,

                                    o.Shipment
                                        .TrackingUrl,

                                    o.Shipment
                                        .ExpectedDeliveryDate,

                                    o.Shipment
                                        .DispatchNote,

                                    o.Shipment
                                        .Status,

                                    o.Shipment
                                        .HandedOverAt,

                                    o.Shipment
                                        .DeliveredAt
                                },

                        FulfillmentHistory =
                            o.FulfillmentStatusHistory
                                .OrderBy(h =>
                                    h.CreatedAt)
                                .Select(h =>
                                    new
                                    {
                                        h.PreviousStatus,

                                        h.NewStatus,

                                        ChangedBy =
                                            h.ChangedByUser != null

                                                ? h.ChangedByUser
                                                    .FullName

                                                : "System",

                                        h.Note,

                                        h.CreatedAt
                                    }
                                )
                                .ToList()
                    }
                )
                .FirstOrDefaultAsync(
                    cancellationToken
                );


        if (
            order == null
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetOrderDetails,
                "The order was not found for the authenticated buyer."
            );
        }


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames.GetOrderDetails,

            Success =
                true,

            Message =
                "Order details retrieved successfully.",

            Data =
                order
        };
    }


    // ============================================================
    // TOOL 6
    // GET SHIPMENT / COURIER TRACKING
    // ============================================================

    private async Task<MarketplaceAgentToolResultDto>
        GetShipmentTrackingToolAsync(
            Guid buyerId,
            JsonElement? arguments,
            CancellationToken cancellationToken)
    {
        var orderId =
            GetGuid(
                arguments,
                "orderId"
            );


        if (
            !orderId.HasValue
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetShipmentTracking,
                "A valid order ID is required."
            );
        }


        var order =
            await _context.Orders
                .AsNoTracking()
                .Where(o =>
                    o.Id ==
                        orderId.Value
                    &&
                    o.BuyerId ==
                        buyerId
                )
                .Select(o =>
                    new
                    {
                        o.Id,

                        GemTitle =
                            o.GemListing != null

                                ? o.GemListing.Title

                                : string.Empty,

                        o.Status,

                        o.FulfillmentStatus,

                        o.HandedOverAt,

                        o.DeliveredAt,

                        Shipment =
                            o.Shipment == null

                                ? null

                                : new
                                {
                                    o.Shipment
                                        .CourierName,

                                    o.Shipment
                                        .TrackingNumber,

                                    o.Shipment
                                        .TrackingUrl,

                                    o.Shipment
                                        .ExpectedDeliveryDate,

                                    o.Shipment
                                        .DispatchNote,

                                    o.Shipment
                                        .Status,

                                    o.Shipment
                                        .HandedOverAt,

                                    o.Shipment
                                        .DeliveredAt
                                }
                    }
                )
                .FirstOrDefaultAsync(
                    cancellationToken
                );


        if (
            order == null
        )
        {
            return FailedToolResult(
                MarketplaceAgentToolNames.GetShipmentTracking,
                "The order was not found for the authenticated buyer."
            );
        }


        if (
            order.Shipment == null
        )
        {
            return new MarketplaceAgentToolResultDto
            {
                ToolName =
                    MarketplaceAgentToolNames
                        .GetShipmentTracking,

                Success =
                    true,

                Message =
                    "The order exists, but courier shipment information is not available yet.",

                Data =
                    order
            };
        }


        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                MarketplaceAgentToolNames
                    .GetShipmentTracking,

            Success =
                true,

            Message =
                "Shipment tracking information retrieved successfully.",

            Data =
                order
        };
    }


    // ============================================================
    // EXISTING LEGACY RECOMMENDATION ENDPOINT
    //
    // IMPORTANT:
    //
    // This keeps the old Buyer Assistant endpoint working.
    //
    // The removed IMarketplaceAiModelClient is NOT required.
    // This method uses safe backend-filtered marketplace data.
    // ============================================================

    public async Task<LegacyResponse> RecommendAsync(
        Guid buyerId,
        LegacyRequest request,
        CancellationToken cancellationToken = default)
    {
        await ValidateBuyerAsync(
            buyerId,
            cancellationToken
        );


        if (
            string.IsNullOrWhiteSpace(
                request.Query)
        )
        {
            throw new InvalidOperationException(
                "Please describe what kind of gemstone you are looking for."
            );
        }


        if (
            request.MaxPrice.HasValue &&
            request.MaxPrice.Value < 0
        )
        {
            throw new InvalidOperationException(
                "Maximum price cannot be negative."
            );
        }


        // --------------------------------------------------------
        // APPROVED + CURRENTLY AVAILABLE
        //
        // No AvailabilityStatus property is used.
        // --------------------------------------------------------

        var query =
            _context.GemListings
                .AsNoTracking()
                .Where(g =>
                    g.Status ==
                        GemListingStatuses.Approved
                    &&
                    !g.Orders.Any(o =>
                        o.Status !=
                            OrderStatuses.Cancelled
                        &&
                        o.Status !=
                            OrderStatuses.Refunded
                        &&
                        o.Status !=
                            OrderStatuses.Failed
                    )
                );


        // --------------------------------------------------------
        // GEM TYPE
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                request.GemType)
        )
        {
            var value =
                request.GemType
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.GemType
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // COLOR
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                request.Color)
        )
        {
            var value =
                request.Color
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.Color
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // CUT
        // --------------------------------------------------------

        if (
            !string.IsNullOrWhiteSpace(
                request.Cut)
        )
        {
            var value =
                request.Cut
                    .Trim()
                    .ToLower();


            query =
                query.Where(g =>
                    g.Cut
                        .ToLower()
                        .Contains(value)
                );
        }


        // --------------------------------------------------------
        // MAX PRICE
        // --------------------------------------------------------

        if (
            request.MaxPrice.HasValue
        )
        {
            query =
                query.Where(g =>
                    g.Price <=
                        request.MaxPrice.Value
                );
        }


        // --------------------------------------------------------
        // QUERY
        // --------------------------------------------------------

        var eligible =
            await query
                .OrderByDescending(g =>
                    g.CreatedAt
                )
                .Take(10)
                .Select(g =>
                    new
                    {
                        g.Id,

                        g.Title,

                        g.GemType,

                        g.Color,

                        g.Clarity,

                        g.Cut,

                        g.CaratWeight,

                        g.Price,

                        g.Currency,

                        g.PrimaryImageUrl
                    }
                )
                .ToListAsync(
                    cancellationToken
                );


        var response =
            new LegacyResponse
            {
                FiltersApplied =
                    BuildLegacyFilterSummary(
                        request
                    )
            };


        if (
            eligible.Count == 0
        )
        {
            response.Summary =
                "No currently approved and available gemstones match those preferences.";

            response.Warnings.Add(
                "Try widening the gemstone type, color, cut, or maximum price."
            );

            response.UsedFallback =
                false;

            return response;
        }


        response.Summary =
            "Gemora found approved and currently available gemstones matching your preferences.";


        response.Recommendations =
            eligible
                .Take(5)
                .Select(g =>
                    new MarketplaceAgentRecommendationDto
                    {
                        GemListingId =
                            g.Id,

                        Title =
                            g.Title,

                        Price =
                            g.Price,

                        Currency =
                            g.Currency,

                        PrimaryImageUrl =
                            g.PrimaryImageUrl,

                        Reasons =
                            new List<string>
                            {
                                $"{g.GemType} gemstone",
                                $"{g.CaratWeight:0.##} ct",
                                $"{g.Color} color",
                                $"{g.Cut} cut",
                                "Approved and currently available in Gemora"
                            },

                        Tradeoffs =
                            new List<string>
                            {
                                "Review the full gemstone details and certificate information before purchasing."
                            }
                    }
                )
                .ToList();


        response.UsedFallback =
            false;


        return response;
    }


    // ============================================================
    // AUTHENTICATED BUYER VALIDATION
    // ============================================================

    private async Task ValidateBuyerAsync(
        Guid buyerId,
        CancellationToken cancellationToken)
    {
        var buyerExists =
            await _context.Users
                .AsNoTracking()
                .AnyAsync(
                    u =>
                        u.Id ==
                            buyerId
                        &&
                        u.Role ==
                            UserRoles.Buyer,

                    cancellationToken
                );


        if (
            !buyerExists
        )
        {
            throw new UnauthorizedAccessException(
                "Only authenticated buyers can use the marketplace assistant."
            );
        }
    }


    // ============================================================
    // GEMINI SYSTEM PROMPT
    // ============================================================

    private static string BuildSystemPrompt()
    {
        return
            """
            You are Gemora's Marketplace and Buyer Assistance Agent.

            Gemora is a secure gemstone marketplace.

            You assist authenticated gemstone buyers using real
            Gemora marketplace and transaction information.

            RESPONSIBILITIES:

            1. Search approved and currently available gemstones.
            2. Help buyers discover gemstones using natural language.
            3. Explain gemstone listing information.
            4. Compare currently available gemstones.
            5. Retrieve the authenticated buyer's real orders.
            6. Explain order status.
            7. Explain payment status.
            8. Explain fulfillment status.
            9. Retrieve courier and shipment tracking information.
            10. Provide general gemstone marketplace guidance.

            MARKETPLACE RULES:

            - Marketplace facts must come from Gemora backend data.
            - Only approved gemstones can be marketplace listings.
            - A gemstone with an active order is not available
              for another purchase.
            - Cancelled, Refunded and Failed orders do not block
              a gemstone from being available again.
            - A sold/unavailable approved gemstone may still be
              displayed when required for historical order details.

            SECURITY RULES:

            - Never invent gemstone listings.
            - Never invent listing IDs.
            - Never invent prices.
            - Never invent gemstone specifications.
            - Never invent certificate information.
            - Never invent seller information.
            - Never invent buyer orders.
            - Never invent payment status.
            - Never invent fulfillment status.
            - Never invent shipment information.
            - Never invent courier details.
            - Never invent tracking numbers.

            - Never claim payment succeeded unless verified backend
              information confirms the payment.

            - Never modify payment state.
            - Never modify fulfillment state.
            - Never dispatch an order.
            - Never mark an order delivered.
            - Never issue refunds.
            - Never cancel an order.
            - Never directly modify the database.

            - Never expose API keys.
            - Never expose JWT tokens.
            - Never expose passwords.
            - Never expose database credentials.
            - Never expose internal secrets.

            AUTHORIZATION RULES:

            - Never trust a buyer ID from user text.
            - Buyer identity comes from authenticated JWT claims.
            - Only retrieve order information belonging to the
              authenticated buyer.
            - Never reveal another buyer's private order information.

            TOOL RULES:

            - Use backend tools for Gemora marketplace facts.
            - Use backend tools for transaction facts.
            - Use backend tools for shipment facts.
            - If backend data is unavailable, say that clearly.
            - Never guess missing marketplace or transaction data.

            GENERAL EDUCATION:

            You may answer general gemstone education questions
            without a backend tool.

            Clearly distinguish general educational information
            from actual Gemora marketplace data.

            Keep responses concise, professional, clear and helpful.
            """;
    }


    // ============================================================
    // LEGACY FILTER SUMMARY
    // ============================================================

    private static Dictionary<string, string>
        BuildLegacyFilterSummary(
            LegacyRequest request)
    {
        var filters =
            new Dictionary<string, string>();


        if (
            !string.IsNullOrWhiteSpace(
                request.GemType)
        )
        {
            filters["gemType"] =
                request.GemType.Trim();
        }


        if (
            request.MaxPrice.HasValue
        )
        {
            filters["maxPrice"] =
                request.MaxPrice
                    .Value
                    .ToString("0.##");
        }


        if (
            !string.IsNullOrWhiteSpace(
                request.Color)
        )
        {
            filters["color"] =
                request.Color.Trim();
        }


        if (
            !string.IsNullOrWhiteSpace(
                request.Cut)
        )
        {
            filters["cut"] =
                request.Cut.Trim();
        }


        return filters;
    }


    // ============================================================
    // JSON HELPER
    // STRING
    // ============================================================

    private static string?
        GetString(
            JsonElement? arguments,
            string propertyName)
    {
        if (
            !arguments.HasValue ||
            arguments.Value.ValueKind !=
                JsonValueKind.Object ||
            !arguments.Value.TryGetProperty(
                propertyName,
                out var element)
        )
        {
            return null;
        }


        if (
            element.ValueKind !=
                JsonValueKind.String
        )
        {
            return null;
        }


        return element
            .GetString();
    }


    // ============================================================
    // JSON HELPER
    // DECIMAL
    // ============================================================

    private static decimal?
        GetDecimal(
            JsonElement? arguments,
            string propertyName)
    {
        if (
            !arguments.HasValue ||
            arguments.Value.ValueKind !=
                JsonValueKind.Object ||
            !arguments.Value.TryGetProperty(
                propertyName,
                out var element)
        )
        {
            return null;
        }


        if (
            element.ValueKind ==
                JsonValueKind.Number &&
            element.TryGetDecimal(
                out var value)
        )
        {
            return value;
        }


        if (
            element.ValueKind ==
                JsonValueKind.String &&
            decimal.TryParse(
                element.GetString(),
                out value)
        )
        {
            return value;
        }


        return null;
    }


    // ============================================================
    // JSON HELPER
    // INTEGER
    // ============================================================

    private static int?
        GetInt(
            JsonElement? arguments,
            string propertyName)
    {
        if (
            !arguments.HasValue ||
            arguments.Value.ValueKind !=
                JsonValueKind.Object ||
            !arguments.Value.TryGetProperty(
                propertyName,
                out var element)
        )
        {
            return null;
        }


        if (
            element.ValueKind ==
                JsonValueKind.Number &&
            element.TryGetInt32(
                out var value)
        )
        {
            return value;
        }


        if (
            element.ValueKind ==
                JsonValueKind.String &&
            int.TryParse(
                element.GetString(),
                out value)
        )
        {
            return value;
        }


        return null;
    }


    // ============================================================
    // JSON HELPER
    // GUID
    // ============================================================

    private static Guid?
        GetGuid(
            JsonElement? arguments,
            string propertyName)
    {
        var value =
            GetString(
                arguments,
                propertyName
            );


        if (
            Guid.TryParse(
                value,
                out var id)
        )
        {
            return id;
        }


        return null;
    }


    // ============================================================
    // JSON HELPER
    // INTEGER ARRAY
    // ============================================================

    private static List<int>
        GetIntArray(
            JsonElement? arguments,
            string propertyName)
    {
        var result =
            new List<int>();


        if (
            !arguments.HasValue ||
            arguments.Value.ValueKind !=
                JsonValueKind.Object ||
            !arguments.Value.TryGetProperty(
                propertyName,
                out var element) ||
            element.ValueKind !=
                JsonValueKind.Array
        )
        {
            return result;
        }


        foreach (
            var item in
            element.EnumerateArray()
        )
        {
            if (
                item.ValueKind ==
                    JsonValueKind.Number &&
                item.TryGetInt32(
                    out var id)
            )
            {
                result.Add(
                    id
                );

                continue;
            }


            if (
                item.ValueKind ==
                    JsonValueKind.String &&
                int.TryParse(
                    item.GetString(),
                    out id)
            )
            {
                result.Add(
                    id
                );
            }
        }


        return result;
    }


    // ============================================================
    // FAILED TOOL RESULT
    // ============================================================

    private static MarketplaceAgentToolResultDto
        FailedToolResult(
            string toolName,
            string message)
    {
        return new MarketplaceAgentToolResultDto
        {
            ToolName =
                toolName,

            Success =
                false,

            Message =
                message,

            Data =
                null
        };
    }
}