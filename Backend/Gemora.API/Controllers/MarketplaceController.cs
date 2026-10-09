using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace Gemora.API.Controllers;

[ApiController]
[Route("api/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceService _service;
    public MarketplaceController(IMarketplaceService service) => _service = service;

    [AllowAnonymous]
[Route("api/[controller]")]
[AllowAnonymous]
public class MarketplaceController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public MarketplaceController(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // PUBLIC MARKETPLACE LIST
    //
    // GET:
    // /api/Marketplace/gems
    //
    // Optional:
    // /api/Marketplace/gems?search=sapphire
    // /api/Marketplace/gems?gemType=Ruby
    // /api/Marketplace/gems?search=blue&gemType=Sapphire
    //
    // IMPORTANT:
    // Only Approved listings are exposed publicly.
    // ============================================================

    [HttpGet("gems")]
    public async Task<ActionResult<PagedMarketplaceResponseDto>> Search([FromQuery] MarketplaceSearchQueryDto query)
        => Ok(await _service.SearchAsync(query));
    public async Task<ActionResult<List<MarketplaceGemDto>>> GetGems(
        [FromQuery] string? search = null,
        [FromQuery] string? gemType = null)
    {
        var query =
            _context.GemListings
                .AsNoTracking()
                .Where(g =>
                    g.Status == GemListingStatuses.Approved);

        // --------------------------------------------------------
        // SEARCH
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch =
                search.Trim().ToLower();

            query =
                query.Where(g =>
                    g.Title.ToLower()
                        .Contains(cleanSearch) ||

                    g.GemType.ToLower()
                        .Contains(cleanSearch) ||

                    g.Color.ToLower()
                        .Contains(cleanSearch) ||

                    g.Cut.ToLower()
                        .Contains(cleanSearch) ||

                    g.Description.ToLower()
                        .Contains(cleanSearch));
        }

        // --------------------------------------------------------
        // GEM TYPE FILTER
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(gemType) &&
            !string.Equals(
                gemType,
                "All",
                StringComparison.OrdinalIgnoreCase))
        {
            var cleanGemType =
                gemType.Trim().ToLower();

            query =
                query.Where(g =>
                    g.GemType.ToLower()
                        .Contains(cleanGemType));
        }

        // --------------------------------------------------------
        // RESULT
        // --------------------------------------------------------

        var gems =
            await query
                .OrderByDescending(
                    g => g.UpdatedAt ?? g.CreatedAt)
                .Select(
                    g => new MarketplaceGemDto
                    {
                        Id = g.Id,

                        SellerName =
                            g.Seller.FullName,

                        Title = g.Title,

                        GemType = g.GemType,

                        Description =
                            g.Description,

                        CaratWeight =
                            g.CaratWeight,

                        Color = g.Color,

                        Clarity = g.Clarity,

                        Cut = g.Cut,

                        Price = g.Price,

                        Currency =
                            g.Currency,

                        PrimaryImageUrl =
                            g.PrimaryImageUrl,

                        CertificateNumber =
                            g.CertificateNumber,

                        CertificateAuthority =
                            g.CertificateAuthority,

                        Status = g.Status,

                        CreatedAt =
                            g.CreatedAt,

                        UpdatedAt =
                            g.UpdatedAt
                    })
                .ToListAsync();

        return Ok(gems);
    }

    // ============================================================
    // PUBLIC MARKETPLACE GEM DETAILS
    //
    // GET:
    // /api/Marketplace/gems/{id}
    //
    // CertificateUrl is deliberately NOT exposed.
    // ============================================================

    [AllowAnonymous]
    [HttpGet("gems/{id:int}")]
    public async Task<ActionResult<MarketplaceGemResponseDto>> GetById(int id)
    public async Task<ActionResult<MarketplaceGemDto>>
        GetGemById(
            int id)
    {
        var gem = await _service.GetByIdAsync(id);
        return gem == null ? NotFound(new { message = "Marketplace gem was not found or is unavailable." }) : Ok(gem);
    }
        var gem =
            await _context.GemListings
                .AsNoTracking()
                .Where(
                    g =>
                        g.Id == id &&
                        g.Status ==
                            GemListingStatuses.Approved)
                .Select(
                    g => new MarketplaceGemDto
                    {
                        Id = g.Id,

                        SellerName =
                            g.Seller.FullName,

                        Title = g.Title,

                        GemType =
                            g.GemType,

                        Description =
                            g.Description,

                        CaratWeight =
                            g.CaratWeight,

                        Color =
                            g.Color,

    [HttpGet("stats")]
    [AllowAnonymous]
    public async Task<IActionResult> GetStats()
    {
        return Ok(
            await _service.GetStatsAsync()
        );
                        Clarity =
                            g.Clarity,

                        Cut =
                            g.Cut,

                        Price =
                            g.Price,

                        Currency =
                            g.Currency,

                        PrimaryImageUrl =
                            g.PrimaryImageUrl,

                        CertificateNumber =
                            g.CertificateNumber,

                        CertificateAuthority =
                            g.CertificateAuthority,

                        Status =
                            g.Status,

                        CreatedAt =
                            g.CreatedAt,

                        UpdatedAt =
                            g.UpdatedAt
                    })
                .FirstOrDefaultAsync();

        if (gem == null)
        {
            return NotFound(
                new
                {
                    message =
                        "This gemstone is not available in the marketplace."
                });
        }

        return Ok(gem);
    }
}

// ================================================================
// PUBLIC MARKETPLACE DTO
//
// Do NOT expose:
// - SellerId
// - CertificateUrl
// - private storage references
// - internal verification records
// ================================================================

public class MarketplaceGemDto
{
    public int Id { get; set; }

    public string SellerName { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string GemType { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public decimal CaratWeight { get; set; }

    public string Color { get; set; } =
        string.Empty;

    public string Clarity { get; set; } =
        string.Empty;

    public string Cut { get; set; } =
        string.Empty;

    public decimal Price { get; set; }

    public string Currency { get; set; } =
        "LKR";

    public string? PrimaryImageUrl { get; set; }

    public string? CertificateNumber { get; set; }

    public string? CertificateAuthority { get; set; }

    public string Status { get; set; } =
        string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}