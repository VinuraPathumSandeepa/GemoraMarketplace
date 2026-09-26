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
        // Make sure the authenticated user is actually a Seller.
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
            Currency = dto.Currency.Trim().ToUpperInvariant(),

            // Every new listing starts as Draft.
            Status = GemListingStatuses.Draft,

            CreatedAt = DateTime.UtcNow
        };

        _context.GemListings.Add(listing);

        await _context.SaveChangesAsync();

        return await GetListingDtoAsync(listing.Id);
    }


    // ============================================================
    // GET ALL LISTINGS OF CURRENT SELLER
    // ============================================================

    public async Task<List<GemListingDto>> GetMyListingsAsync(
        Guid sellerId)
    {
        return await _context.GemListings
            .AsNoTracking()
            .Where(g => g.SellerId == sellerId)
            .OrderByDescending(g => g.CreatedAt)
            .Select(g => new GemListingDto
            {
                Id = g.Id,
                SellerId = g.SellerId,
                SellerName = g.Seller.FullName,
                Title = g.Title,
                GemType = g.GemType,
                Description = g.Description,
                CaratWeight = g.CaratWeight,
                Color = g.Color,
                Clarity = g.Clarity,
                Cut = g.Cut,
                Price = g.Price,
                Currency = g.Currency,
                Status = g.Status,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToListAsync();
    }


    // ============================================================
    // GET ONE LISTING OF CURRENT SELLER
    // ============================================================

    public async Task<GemListingDto?> GetByIdAsync(
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
                SellerName = g.Seller.FullName,
                Title = g.Title,
                GemType = g.GemType,
                Description = g.Description,
                CaratWeight = g.CaratWeight,
                Color = g.Color,
                Clarity = g.Clarity,
                Cut = g.Cut,
                Price = g.Price,
                Currency = g.Currency,
                Status = g.Status,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }


    // ============================================================
    // UPDATE GEM LISTING
    // ============================================================

    public async Task<bool> UpdateAsync(
        int id,
        Guid sellerId,
        UpdateGemListingDto dto)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(g =>
                g.Id == id &&
                g.SellerId == sellerId);

        if (listing == null)
        {
            return false;
        }

        // Seller can only edit Draft or ChangesRequested listings.
        if (listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be edited.");
        }

        listing.Title = dto.Title.Trim();
        listing.GemType = dto.GemType.Trim();
        listing.Description = dto.Description.Trim();
        listing.CaratWeight = dto.CaratWeight;
        listing.Color = dto.Color.Trim();
        listing.Clarity = dto.Clarity.Trim();
        listing.Cut = dto.Cut.Trim();
        listing.Price = dto.Price;
        listing.Currency = dto.Currency.Trim().ToUpperInvariant();
        listing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }


    // ============================================================
    // DELETE GEM LISTING
    // ============================================================

    public async Task<bool> DeleteAsync(
        int id,
        Guid sellerId)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(g =>
                g.Id == id &&
                g.SellerId == sellerId);

        if (listing == null)
        {
            return false;
        }

        // Only Draft listings can be deleted.
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
    // SUBMIT GEM LISTING FOR VERIFICATION
    // ============================================================

    public async Task<GemListingDto?> SubmitForVerificationAsync(
        int id,
        Guid sellerId)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(g =>
                g.Id == id &&
                g.SellerId == sellerId);

        if (listing == null)
        {
            return null;
        }

        // Only Draft or ChangesRequested listings can be submitted.
        if (listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be submitted for verification.");
        }

        // --------------------------------------------------------
        // STEP 1:
        // Change listing status to PendingVerification.
        // --------------------------------------------------------

        listing.Status =
            GemListingStatuses.PendingVerification;

        listing.UpdatedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // STEP 2:
        // Create a new verification record.
        //
        // Every submission gets its own record. This means that
        // if the Gemologist requests changes and the Seller
        // resubmits, we preserve the previous verification history.
        // --------------------------------------------------------

        var verification = new GemVerification
        {
            GemListingId = listing.Id,

            // No human Gemologist has reviewed it yet.
            GemologistId = null,

            // Human decision has not been made yet.
            Decision = "Pending",

            ReviewNotes = null,

            // AI processing will be implemented later.
            AiSuggestedGemType = null,

            AiConfidenceScore = null,

            AiFindings = null,

            AiRiskFlags = null,

            AiStatus = "NotStarted",

            CreatedAt = DateTime.UtcNow,

            AiProcessedAt = null,

            ReviewedAt = null
        };

        _context.GemVerifications.Add(verification);


        // --------------------------------------------------------
        // STEP 3:
        // Save both:
        //
        // GemListing -> PendingVerification
        // GemVerification -> Pending
        //
        // EF Core performs both changes in the same SaveChanges.
        // --------------------------------------------------------

        await _context.SaveChangesAsync();


        // --------------------------------------------------------
        // STEP 4:
        // Return updated listing.
        // --------------------------------------------------------

        return await GetListingDtoAsync(listing.Id);
    }


    // ============================================================
    // PRIVATE HELPER
    // GET LISTING AND MAP ENTITY -> DTO
    // ============================================================

    private async Task<GemListingDto> GetListingDtoAsync(
        int listingId)
    {
        return await _context.GemListings
            .AsNoTracking()
            .Where(g => g.Id == listingId)
            .Select(g => new GemListingDto
            {
                Id = g.Id,

                SellerId = g.SellerId,

                SellerName = g.Seller.FullName,

                Title = g.Title,

                GemType = g.GemType,

                Description = g.Description,

                CaratWeight = g.CaratWeight,

                Color = g.Color,

                Clarity = g.Clarity,

                Cut = g.Cut,

                Price = g.Price,

                Currency = g.Currency,

                Status = g.Status,

                CreatedAt = g.CreatedAt,

                UpdatedAt = g.UpdatedAt
            })
            .SingleAsync();
    }
}