using Gemora.Application.DTOs.Marketplace;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
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
    // ONLY:
    // Approved + currently purchasable gemstones.
    //
    // Pending requests remain public; seller approval reserves the unique gemstone.
    // =========================================================

    public async Task<PagedMarketplaceResponseDto>
        SearchAsync(
            MarketplaceSearchQueryDto query)
    {
        var gems =
            AvailableListings();


        // =====================================================
        // SEARCH
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                query.Search))
        {
            var search =
                query.Search.Trim();

            var pattern =
                $"%{search}%";

            gems =
                gems.Where(g =>
                    EF.Functions.ILike(
                        g.Title,
                        pattern) ||

                    EF.Functions.ILike(
                        g.GemType,
                        pattern) ||

                    EF.Functions.ILike(
                        g.Description,
                        pattern));
        }


        // =====================================================
        // GEM TYPE
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                query.GemType))
        {
            var gemType =
                query.GemType.Trim();

            gems =
                gems.Where(g =>
                    EF.Functions.ILike(
                        g.GemType,
                        gemType));
        }


        // =====================================================
        // COLOR
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                query.Color))
        {
            var color =
                query.Color.Trim();

            gems =
                gems.Where(g =>
                    EF.Functions.ILike(
                        g.Color,
                        color));
        }


        // =====================================================
        // CUT
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                query.Cut))
        {
            var cut =
                query.Cut.Trim();

            gems =
                gems.Where(g =>
                    EF.Functions.ILike(
                        g.Cut,
                        cut));
        }


        // =====================================================
        // COUNTRY
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                query.CountryCode))
        {
            var country =
                query.CountryCode
                    .Trim()
                    .ToUpperInvariant();

            gems =
                gems.Where(g =>
                    g.CountryCode ==
                    country);
        }


        // =====================================================
        // PRICE RANGE
        // =====================================================

        if (
            query.MinPrice.HasValue)
        {
            gems =
                gems.Where(g =>
                    g.Price >=
                    query.MinPrice.Value);
        }

        if (
            query.MaxPrice.HasValue)
        {
            gems =
                gems.Where(g =>
                    g.Price <=
                    query.MaxPrice.Value);
        }


        // =====================================================
        // CARAT RANGE
        // =====================================================

        if (
            query.MinCarat.HasValue)
        {
            gems =
                gems.Where(g =>
                    g.CaratWeight >=
                    query.MinCarat.Value);
        }

        if (
            query.MaxCarat.HasValue)
        {
            gems =
                gems.Where(g =>
                    g.CaratWeight <=
                    query.MaxCarat.Value);
        }


        // =====================================================
        // SORTING
        // =====================================================

        var sort =
            query.Sort?
                .Trim()
                .ToLowerInvariant()
            ?? string.Empty;

        gems =
            sort switch
            {
                "price_asc" =>
                    gems.OrderBy(g =>
                        g.Price),

                "price_desc" =>
                    gems.OrderByDescending(g =>
                        g.Price),

                "carat_asc" =>
                    gems.OrderBy(g =>
                        g.CaratWeight),

                "carat_desc" =>
                    gems.OrderByDescending(g =>
                        g.CaratWeight),

                "oldest" =>
                    gems.OrderBy(g =>
                        g.CreatedAt),

                _ =>
                    gems.OrderByDescending(g =>
                        g.CreatedAt)
            };


        // =====================================================
        // SAFE PAGINATION
        // =====================================================

        var page =
            query.Page < 1
                ? 1
                : query.Page;

        var pageSize =
            query.PageSize < 1
                ? 12
                : Math.Min(
                    query.PageSize,
                    50);


        // =====================================================
        // COUNT
        // =====================================================

        var totalItems =
            await gems.CountAsync();


        // =====================================================
        // RESULTS
        // =====================================================

        var items =
            await gems
                .Skip(
                    (page - 1) *
                    pageSize)

                .Take(
                    pageSize)

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

                        // AvailableListings()
                        // guarantees this.
                        IsAvailable =
                            true
                    })
                .ToListAsync();


        return new PagedMarketplaceResponseDto
        {
            Items =
                items,

            Page =
                page,

            PageSize =
                pageSize,

            TotalItems =
                totalItems,

            TotalPages =
                totalItems == 0
                    ? 0
                    : (int)Math.Ceiling(
                        totalItems /
                        (double)pageSize)
        };
    }


    // =========================================================
    // SINGLE GEM
    //
    // IMPORTANT:
    //
    // Sold/reserved gems remain viewable via:
    //
    // My Orders → View Gem
    //
    // therefore GetById uses ApprovedListings(),
    // not AvailableListings().
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


                    IsAvailable =
                        !g.Orders.Any(o =>
                    o.Status != OrderStatuses.Pending && o.Status != OrderStatuses.Rejected &&

                            o.Status !=
                                OrderStatuses
                                    .Cancelled &&

                            o.Status !=
                                OrderStatuses
                                    .Refunded &&

                            o.Status !=
                                OrderStatuses
                                    .Failed)
                })

            .FirstOrDefaultAsync();
    }


    // =========================================================
    // APPROVED LISTINGS
    //
    // Used for:
    // - historical / order gem details
    // =========================================================

    private IQueryable<GemListing>
        ApprovedListings()
    {
        return _context
            .GemListings
            .AsNoTracking()
            .Where(g =>
                g.Status ==
                    GemListingStatuses
                        .Approved);
    }


    // =========================================================
    // AVAILABLE MARKETPLACE LISTINGS
    //
    // Unique gemstone rule:
    //
    // Cancelled  → available again
    // Refunded   → available again
    // Failed     → available again
    //
    // Confirmed / Paid /
    // Completed etc. → reserved/sold.
    // =========================================================

    private IQueryable<GemListing>
        AvailableListings()
    {
        return ApprovedListings()

            .Where(g =>
                !g.Orders.Any(o =>
                    o.Status != OrderStatuses.Pending && o.Status != OrderStatuses.Rejected &&

                    o.Status !=
                        OrderStatuses
                            .Cancelled &&

                    o.Status !=
                        OrderStatuses
                            .Refunded &&

                    o.Status !=
                        OrderStatuses
                            .Failed
                ));
    }


    // =========================================================
    // MARKETPLACE STATS
    // =========================================================

    public async Task<MarketplaceStatsDto>
        GetStatsAsync()
    {
        var authorizedSellers =
            await _context.Users
                .AsNoTracking()
                .CountAsync(u =>
                    u.Role ==
                        UserRoles.Seller &&

                    u.IsEmailVerified);


        var registeredBuyers =
            await _context.Users
                .AsNoTracking()
                .CountAsync(u =>
                    u.Role ==
                    UserRoles.Buyer);


        var activeGemListings =
            await AvailableListings()
                .CountAsync();


        var successfulTransactions =
            await _context.Orders
                .AsNoTracking()
                .CountAsync(o =>

                    o.Status ==
                        OrderStatuses.Paid ||

                    o.Status ==
                        OrderStatuses.Completed);


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