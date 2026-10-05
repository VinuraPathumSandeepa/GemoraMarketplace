using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class MarketplaceService : IMarketplaceService
{
    private readonly ApplicationDbContext _context;

    public MarketplaceService(
        ApplicationDbContext context)
    {
        _context = context;
    }

    // =========================================================
    // MARKETPLACE SEARCH
    //
    // IMPORTANT:
    // Only Approved + currently available gemstones
    // are shown in the public/buyer marketplace.
    //
    // This allows buyers to:
    // - discover new gemstones
    // - search/filter listings
    // - open gem details
    // - place new orders
    //
    // Gems that already have an active order are not shown
    // as available for another purchase.
    // =========================================================

    public async Task<PagedMarketplaceResponseDto>
        SearchAsync(MarketplaceSearchQueryDto query)
    {
        var gems =
            AvailableListings();

        // -----------------------------------------------------
        // SEARCH
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                query.Search))
        {
            var search =
                query.Search
                    .Trim()
                    .ToLower();

            gems = gems.Where(g =>
                g.Title
                    .ToLower()
                    .Contains(search) ||

                g.GemType
                    .ToLower()
                    .Contains(search) ||

                g.Description
                    .ToLower()
                    .Contains(search));
        }

        // -----------------------------------------------------
        // GEM TYPE
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                query.GemType))
        {
            gems = gems.Where(g =>
                g.GemType ==
                query.GemType.Trim());
        }

        // -----------------------------------------------------
        // COLOR
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                query.Color))
        {
            gems = gems.Where(g =>
                g.Color ==
                query.Color.Trim());
        }

        // -----------------------------------------------------
        // CUT
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                query.Cut))
        {
            gems = gems.Where(g =>
                g.Cut ==
                query.Cut.Trim());
        }

        // -----------------------------------------------------
        // COUNTRY
        // -----------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                query.CountryCode))
        {
            gems = gems.Where(g =>
                g.CountryCode ==
                query.CountryCode
                    .Trim()
                    .ToUpper());
        }

        // -----------------------------------------------------
        // PRICE RANGE
        // -----------------------------------------------------

        if (query.MinPrice.HasValue)
        {
            gems = gems.Where(g =>
                g.Price >=
                query.MinPrice.Value);
        }

        if (query.MaxPrice.HasValue)
        {
            gems = gems.Where(g =>
                g.Price <=
                query.MaxPrice.Value);
        }

        // -----------------------------------------------------
        // CARAT RANGE
        // -----------------------------------------------------

        if (query.MinCarat.HasValue)
        {
            gems = gems.Where(g =>
                g.CaratWeight >=
                query.MinCarat.Value);
        }

        if (query.MaxCarat.HasValue)
        {
            gems = gems.Where(g =>
                g.CaratWeight <=
                query.MaxCarat.Value);
        }

        // -----------------------------------------------------
        // SORTING
        // -----------------------------------------------------

        var sort =
            query.Sort?
                .Trim()
                .ToLowerInvariant()
            ?? "";

        gems = sort switch
        {
            "price_asc" =>
                gems.OrderBy(
                    g => g.Price),

            "price_desc" =>
                gems.OrderByDescending(
                    g => g.Price),

            "carat_desc" =>
                gems.OrderByDescending(
                    g => g.CaratWeight),

            _ =>
                gems.OrderByDescending(
                    g => g.CreatedAt)
        };

        // -----------------------------------------------------
        // TOTAL COUNT
        // -----------------------------------------------------

        var totalItems =
            await gems.CountAsync();

        // -----------------------------------------------------
        // PAGINATION + DTO
        // -----------------------------------------------------

        var items =
            await gems
                .Skip(
                    (query.Page - 1) *
                    query.PageSize)
                .Take(
                    query.PageSize)
                .Select(g =>
                    new MarketplaceGemResponseDto
                    {
                        Id =
                            g.Id,

                        Title =
                            g.Title,

                        GemType =
                            g.GemType,

                        Description =
                            g.Description,

                        CaratWeight =
                            g.CaratWeight,

                        Color =
                            g.Color,

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

                        CountryCode =
                            g.CountryCode,

                        Region =
                            g.Region,

                        SellerName =
                            g.Seller.FullName,

                        // Search uses AvailableListings(),
                        // therefore every returned listing
                        // is currently purchasable.
                        IsAvailable =
                            true
                    })
                .ToListAsync();

        return new PagedMarketplaceResponseDto
        {
            Items =
                items,

            Page =
                query.Page,

            PageSize =
                query.PageSize,

            TotalItems =
                totalItems,

            TotalPages =
                (int)Math.Ceiling(
                    totalItems /
                    (double)query.PageSize)
        };
    }


    // =========================================================
    // SINGLE GEM DETAILS
    //
    // IMPORTANT DIFFERENCE FROM SearchAsync:
    //
    // SearchAsync:
    //   Approved + Available only
    //
    // GetByIdAsync:
    //   Any Approved gemstone can be viewed.
    //
    // This is necessary because:
    //
    // Buyer orders Gem #2
    // -> Gem #2 becomes unavailable
    // -> it disappears from marketplace browsing
    // -> BUT buyer must still be able to open:
    //    My Orders -> View Gem
    //
    // =========================================================

    public async Task<MarketplaceGemResponseDto?>
        GetByIdAsync(int id)
    {
        return await ApprovedListings()
            .Where(g =>
                g.Id == id)
            .Select(g =>
                new MarketplaceGemResponseDto
                {
                    Id =
                        g.Id,

                    Title =
                        g.Title,

                    GemType =
                        g.GemType,

                    Description =
                        g.Description,

                    CaratWeight =
                        g.CaratWeight,

                    Color =
                        g.Color,

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

                    CountryCode =
                        g.CountryCode,

                    Region =
                        g.Region,

                    SellerName =
                        g.Seller.FullName,

                    // Here we calculate availability.
                    //
                    // The gem can still be viewed even
                    // when IsAvailable == false.
                    IsAvailable =
                        !g.Orders.Any(o =>
                            o.Status !=
                                OrderStatuses.Cancelled &&

                            o.Status !=
                                OrderStatuses.Refunded &&

                            o.Status !=
                                OrderStatuses.Failed)
                })
            .FirstOrDefaultAsync();
    }


    // =========================================================
    // ALL APPROVED GEM LISTINGS
    //
    // Used for:
    // - single gem detail lookup
    //
    // Does NOT remove already ordered gems.
    // =========================================================

    private IQueryable<
        Gemora.Domain.Entities.GemListing>
        ApprovedListings()
    {
        return _context
            .GemListings
            .AsNoTracking()
            .Where(g =>
                g.Status ==
                GemListingStatuses.Approved);
    }


    // =========================================================
    // APPROVED + CURRENTLY AVAILABLE LISTINGS
    //
    // Used for:
    // - buyer marketplace
    // - marketplace stats
    //
    // An active order reserves/removes the gemstone
    // from new purchase availability.
    // =========================================================

    private IQueryable<
        Gemora.Domain.Entities.GemListing>
        AvailableListings()
    {
        return ApprovedListings()
            .Where(g =>
                !g.Orders.Any(o =>

                    o.Status !=
                        OrderStatuses.Cancelled &&

                    o.Status !=
                        OrderStatuses.Refunded &&

                    o.Status !=
                        OrderStatuses.Failed
                ));
    }


    // =========================================================
    // MARKETPLACE STATS
    // =========================================================

    public async Task<MarketplaceStatsDto>
        GetStatsAsync()
    {
        // -----------------------------------------------------
        // AUTHORIZED / VERIFIED SELLERS
        // -----------------------------------------------------

        var authorizedSellers =
            await _context.Users
                .AsNoTracking()
                .CountAsync(u =>
                    u.Role ==
                        UserRoles.Seller &&

                    u.IsEmailVerified);

        // -----------------------------------------------------
        // REGISTERED BUYERS
        // -----------------------------------------------------

        var registeredBuyers =
            await _context.Users
                .AsNoTracking()
                .CountAsync(u =>
                    u.Role ==
                    UserRoles.Buyer);

        // -----------------------------------------------------
        // ACTIVE / AVAILABLE MARKETPLACE LISTINGS
        // -----------------------------------------------------

        var activeGemListings =
            await AvailableListings()
                .CountAsync();

        // -----------------------------------------------------
        // SUCCESSFUL TRANSACTIONS
        // -----------------------------------------------------

        var successfulTransactions =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(o =>
                    o.Status ==
                    "Paid");

        return new MarketplaceStatsDto
        {
            AuthorizedSellers =
                authorizedSellers,

            RegisteredBuyers =
                registeredBuyers,

            ActiveGemListings =
                activeGemListings,

            SuccessfulTransactions =
                successfulTransactions
        };
    }
}