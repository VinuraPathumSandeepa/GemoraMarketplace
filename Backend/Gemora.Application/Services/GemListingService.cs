using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemListingService : IGemListingService
{
    private readonly ApplicationDbContext _context;

    public GemListingService(ApplicationDbContext context)
    {
        _context = context;
    }


    // ============================================================
    // CREATE GEM LISTING
    // ============================================================

    public async Task<GemListingDto> CreateAsync(
        Guid sellerId,
        CreateGemListingDto dto)
    {
        var sellerExists = await _context.Users.AnyAsync(
            u => u.Id == sellerId &&
                 u.Role == UserRoles.Seller);

        if (!sellerExists)
        {
            throw new UnauthorizedAccessException(
                "Only sellers can create gem listings.");
        }

        var listing = new GemListing
        {
            SellerId = sellerId,

            Title = dto.Title.Trim(),

            GemType = dto.GemType.Trim(),

            Description = dto.Description.Trim(),

            CaratWeight = dto.CaratWeight,

            Color = dto.Color.Trim(),

            Clarity = dto.Clarity.Trim(),

            Cut = dto.Cut.Trim(),

            Price = dto.Price,

            Currency = dto.Currency
                .Trim()
                .ToUpperInvariant(),

            // ----------------------------------------------------
            // Evidence
            // ----------------------------------------------------

            PrimaryImageUrl =
                CleanOptionalValue(dto.PrimaryImageUrl),

            CertificateNumber =
                CleanOptionalValue(dto.CertificateNumber),

            CertificateAuthority =
                CleanOptionalValue(dto.CertificateAuthority),

            CertificateUrl =
                CleanOptionalValue(dto.CertificateUrl),

            // ----------------------------------------------------
            // Workflow
            // ----------------------------------------------------

            Status = GemListingStatuses.Draft,

            CreatedAt = DateTime.UtcNow
        };

        _context.GemListings.Add(listing);

        await _context.SaveChangesAsync();

        return await GetListingDtoAsync(listing.Id);
    }


    // ============================================================
    // GET CURRENT SELLER'S LISTINGS
    // ============================================================

    public async Task<List<GemListingDto>>
        GetMyListingsAsync(Guid sellerId)
    {
        return await _context.GemListings
            .AsNoTracking()

            .Where(g =>
                g.SellerId == sellerId)

            .OrderByDescending(g =>
                g.CreatedAt)

            .Select(g => new GemListingDto
            {
                Id = g.Id,

                SellerId = g.SellerId,

                SellerName =
                    g.Seller.FullName,

                Title = g.Title,

                GemType = g.GemType,

                Description = g.Description,

                CaratWeight = g.CaratWeight,

                Color = g.Color,

                Clarity = g.Clarity,

                Cut = g.Cut,

                Price = g.Price,

                Currency = g.Currency,

                // -----------------------------------------------
                // Evidence
                // -----------------------------------------------

                PrimaryImageUrl =
                    g.PrimaryImageUrl,

                CertificateNumber =
                    g.CertificateNumber,

                CertificateAuthority =
                    g.CertificateAuthority,

                CertificateUrl =
                    g.CertificateUrl,

                // -----------------------------------------------
                // Workflow
                // -----------------------------------------------

                Status = g.Status,

                CreatedAt = g.CreatedAt,

                UpdatedAt = g.UpdatedAt
            })

            .ToListAsync();
    }


    // ============================================================
    // GET ONE LISTING
    //
    // Seller ownership is enforced here.
    // ============================================================

    public async Task<GemListingDto?>
        GetByIdAsync(
            int id,
            Guid sellerId)
    {
        return await _context.GemListings
            .AsNoTracking()

            .Where(g =>
                g.Id == id &&
                g.SellerId == sellerId)

            .Select(g => new GemListingDto
            {
                Id = g.Id,

                SellerId = g.SellerId,

                SellerName =
                    g.Seller.FullName,

                Title = g.Title,

                GemType = g.GemType,

                Description = g.Description,

                CaratWeight = g.CaratWeight,

                Color = g.Color,

                Clarity = g.Clarity,

                Cut = g.Cut,

                Price = g.Price,

                Currency = g.Currency,

                // -----------------------------------------------
                // Evidence
                // -----------------------------------------------

                PrimaryImageUrl =
                    g.PrimaryImageUrl,

                CertificateNumber =
                    g.CertificateNumber,

                CertificateAuthority =
                    g.CertificateAuthority,

                CertificateUrl =
                    g.CertificateUrl,

                // -----------------------------------------------
                // Workflow
                // -----------------------------------------------

                Status = g.Status,

                CreatedAt = g.CreatedAt,

                UpdatedAt = g.UpdatedAt
            })

            .FirstOrDefaultAsync();
    }


    // ============================================================
    // UPDATE LISTING
    //
    // Only Draft or ChangesRequested listings can be edited.
    // ============================================================

    public async Task<bool> UpdateAsync(
        int id,
        Guid sellerId,
        UpdateGemListingDto dto)
    {
        var listing =
            await _context.GemListings
                .FirstOrDefaultAsync(
                    g =>
                        g.Id == id &&
                        g.SellerId == sellerId);

        if (listing == null)
        {
            return false;
        }

        if (listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be edited.");
        }


        // --------------------------------------------------------
        // Basic information
        // --------------------------------------------------------

        listing.Title =
            dto.Title.Trim();

        listing.GemType =
            dto.GemType.Trim();

        listing.Description =
            dto.Description.Trim();

        listing.CaratWeight =
            dto.CaratWeight;

        listing.Color =
            dto.Color.Trim();

        listing.Clarity =
            dto.Clarity.Trim();

        listing.Cut =
            dto.Cut.Trim();

        listing.Price =
            dto.Price;

        listing.Currency =
            dto.Currency
                .Trim()
                .ToUpperInvariant();


        // --------------------------------------------------------
        // Evidence
        // --------------------------------------------------------

        listing.PrimaryImageUrl =
            CleanOptionalValue(dto.PrimaryImageUrl);

        listing.CertificateNumber =
            CleanOptionalValue(dto.CertificateNumber);

        listing.CertificateAuthority =
            CleanOptionalValue(dto.CertificateAuthority);

        listing.CertificateUrl =
            CleanOptionalValue(dto.CertificateUrl);


        // --------------------------------------------------------
        // Audit
        // --------------------------------------------------------

        listing.UpdatedAt =
            DateTime.UtcNow;


        await _context.SaveChangesAsync();

        return true;
    }


    // ============================================================
    // DELETE LISTING
    //
    // Only Draft listings can be deleted.
    // ============================================================

    public async Task<bool> DeleteAsync(
        int id,
        Guid sellerId)
    {
        var listing =
            await _context.GemListings
                .FirstOrDefaultAsync(
                    g =>
                        g.Id == id &&
                        g.SellerId == sellerId);

        if (listing == null)
        {
            return false;
        }

        if (listing.Status != GemListingStatuses.Draft)
        {
            throw new InvalidOperationException(
                "Only Draft listings can be deleted.");
        }

        _context.GemListings.Remove(listing);

        await _context.SaveChangesAsync();

        return true;
    }


    // ============================================================
    // SUBMIT LISTING FOR VERIFICATION
    //
    // Draft or ChangesRequested
    //        ↓
    // PendingVerification
    //
    // A NEW GemVerification record is created every time the
    // listing is submitted/resubmitted.
    // ============================================================

    public async Task<GemListingDto?>
        SubmitForVerificationAsync(
            int id,
            Guid sellerId)
    {
        var listing =
            await _context.GemListings
                .FirstOrDefaultAsync(
                    g =>
                        g.Id == id &&
                        g.SellerId == sellerId);

        if (listing == null)
        {
            return null;
        }

        if (listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be submitted for verification.");
        }


        // --------------------------------------------------------
        // Update listing workflow
        // --------------------------------------------------------

        listing.Status =
            GemListingStatuses.PendingVerification;

        listing.UpdatedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // Create a NEW verification attempt.
        //
        // This preserves previous verification history.
        // --------------------------------------------------------

        var verification =
            new GemVerification
            {
                GemListingId =
                    listing.Id,

                GemologistId =
                    null,

                Decision =
                    "Pending",

                ReviewNotes =
                    null,

                AiSuggestedGemType =
                    null,

                AiConfidenceScore =
                    null,

                AiFindings =
                    null,

                AiRiskFlags =
                    null,

                AiStatus =
                    "NotStarted",

                CreatedAt =
                    DateTime.UtcNow,

                AiProcessedAt =
                    null,

                ReviewedAt =
                    null
            };

        _context.GemVerifications.Add(
            verification);

        await _context.SaveChangesAsync();

        return await GetListingDtoAsync(
            listing.Id);
    }


    // ============================================================
    // INTERNAL DTO MAPPER
    // ============================================================

    private async Task<GemListingDto>
        GetListingDtoAsync(
            int listingId)
    {
        return await _context.GemListings
            .AsNoTracking()

            .Where(g =>
                g.Id == listingId)

            .Select(g => new GemListingDto
            {
                Id = g.Id,

                SellerId = g.SellerId,

                SellerName =
                    g.Seller.FullName,

                Title = g.Title,

                GemType = g.GemType,

                Description = g.Description,

                CaratWeight = g.CaratWeight,

                Color = g.Color,

                Clarity = g.Clarity,

                Cut = g.Cut,

                Price = g.Price,

                Currency = g.Currency,

                // -----------------------------------------------
                // Evidence
                // -----------------------------------------------

                PrimaryImageUrl =
                    g.PrimaryImageUrl,

                CertificateNumber =
                    g.CertificateNumber,

                CertificateAuthority =
                    g.CertificateAuthority,

                CertificateUrl =
                    g.CertificateUrl,

                // -----------------------------------------------
                // Workflow
                // -----------------------------------------------

                Status = g.Status,

                CreatedAt = g.CreatedAt,

                UpdatedAt = g.UpdatedAt
            })

            .SingleAsync();
    }


    // ============================================================
    // OPTIONAL STRING CLEANER
    //
    // Converts:
    //
    // ""       → null
    // "   "    → null
    // " value " → "value"
    //
    // This keeps optional evidence fields clean in PostgreSQL.
    // ============================================================

    private static string? CleanOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}