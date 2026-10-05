
using Gemora.Application.DTOs.GemListings;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Domain.Entities;
using Gemora.Domain.Interfaces;
using IFileStorageService = Gemora.Domain.Interfaces.IFileStorageService;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemListingService : IGemListingService
{
    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;

    public GemListingService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService)
    {
        _context = context;
        _fileStorageService = fileStorageService;
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
                 u.Role == UserRoles.Seller
        );

        if (!sellerExists)
        {
            throw new UnauthorizedAccessException(
                "Only sellers can create gem listings."
            );
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

            // File URLs must be assigned by the upload endpoints.
            // Never trust a file reference supplied by the create DTO.

            PrimaryImageUrl = null,

            CertificateNumber =
                CleanOptionalValue(dto.CertificateNumber),

            CertificateAuthority =
                CleanOptionalValue(dto.CertificateAuthority),

            CertificateUrl = null,

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

                PrimaryImageUrl = g.PrimaryImageUrl,

                CertificateNumber = g.CertificateNumber,
                CertificateAuthority = g.CertificateAuthority,
                CertificateUrl = g.CertificateUrl,

                Status = g.Status,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .ToListAsync();
    }

    // ============================================================
    // GET ONE LISTING
    // Ownership is enforced using sellerId.
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

                PrimaryImageUrl = g.PrimaryImageUrl,

                CertificateNumber = g.CertificateNumber,
                CertificateAuthority = g.CertificateAuthority,
                CertificateUrl = g.CertificateUrl,

                Status = g.Status,
                CreatedAt = g.CreatedAt,
                UpdatedAt = g.UpdatedAt
            })
            .FirstOrDefaultAsync();
    }

    // ============================================================
    // UPDATE LISTING
    //
    // Important fix:
    // Ordinary edits must not overwrite PrimaryImageUrl
    // or CertificateUrl.
    // ============================================================

    public async Task<bool> UpdateAsync(
        int id,
        Guid sellerId,
        UpdateGemListingDto dto)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(
                g => g.Id == id &&
                     g.SellerId == sellerId
            );

        if (listing == null)
        {
            return false;
        }

        if (
            listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested
        )
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be edited."
            );
        }

        // --------------------------------------------------------
        // Basic gemstone information
        // --------------------------------------------------------

        listing.Title = dto.Title.Trim();

        listing.GemType = dto.GemType.Trim();

        listing.Description = dto.Description.Trim();

        listing.CaratWeight = dto.CaratWeight;

        listing.Color = dto.Color.Trim();

        listing.Clarity = dto.Clarity.Trim();

        listing.Cut = dto.Cut.Trim();

        listing.Price = dto.Price;

        listing.Currency = dto.Currency
            .Trim()
            .ToUpperInvariant();

        // --------------------------------------------------------
        // Certificate metadata
        // --------------------------------------------------------

        listing.CertificateNumber =
            CleanOptionalValue(dto.CertificateNumber);

        listing.CertificateAuthority =
            CleanOptionalValue(dto.CertificateAuthority);

        // --------------------------------------------------------
        // Do NOT update these fields here:
        //
        // listing.PrimaryImageUrl
        // listing.CertificateUrl
        //
        // Only dedicated upload methods can change them.
        // --------------------------------------------------------

        listing.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return true;
    }

    // ============================================================
    // DELETE DRAFT LISTING
    // ============================================================

    public async Task<bool> DeleteAsync(
        int id,
        Guid sellerId)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(
                g => g.Id == id &&
                     g.SellerId == sellerId
            );

        if (listing == null)
        {
            return false;
        }

        if (listing.Status != GemListingStatuses.Draft)
        {
            throw new InvalidOperationException(
                "Only Draft listings can be deleted."
            );
        }

        // Remember file references before deleting the record.

        var imageUrl = listing.PrimaryImageUrl;

        var certificateUrl = listing.CertificateUrl;

        _context.GemListings.Remove(listing);

        await _context.SaveChangesAsync();

        // Current storage service handles deletion.
        // This contract will be retained for Supabase Storage.

        await _fileStorageService.DeleteFileAsync(imageUrl);

        await _fileStorageService.DeleteFileAsync(certificateUrl);

        return true;
    }

    // ============================================================
    // SUBMIT FOR VERIFICATION
    //
    // Draft / ChangesRequested
    //             ↓
    // PendingVerification
    //
    // Every submission creates a NEW verification attempt.
    // ============================================================

    public async Task<GemListingDto?> SubmitForVerificationAsync(
        int id,
        Guid sellerId)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(
                g => g.Id == id &&
                     g.SellerId == sellerId
            );

        if (listing == null)
        {
            return null;
        }

        if (
            listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested
        )
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be submitted for verification."
            );
        }

        // --------------------------------------------------------
        // Update listing status
        // --------------------------------------------------------

        listing.Status =
            GemListingStatuses.PendingVerification;

        listing.UpdatedAt = DateTime.UtcNow;

        // --------------------------------------------------------
        // Create new verification record
        // --------------------------------------------------------

        var verification = new GemVerification
        {
            GemListingId = listing.Id,

            GemologistId = null,

            Decision = "Pending",

            ReviewNotes = null,

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

        await _context.SaveChangesAsync();

        return await GetListingDtoAsync(listing.Id);
    }

    // ============================================================
    // UPLOAD / REPLACE PRIMARY GEMSTONE IMAGE
    //
    // Works through IFileStorageService.
    // The storage implementation can later be switched
    // from local storage to Supabase without changing
    // this service or the API route.
    // ============================================================

    public async Task<GemListingDto?> UploadGemImageAsync(
        int id,
        Guid sellerId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(
                g => g.Id == id &&
                     g.SellerId == sellerId
            );

        if (listing == null)
        {
            return null;
        }

        if (
            listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested
        )
        {
            throw new InvalidOperationException(
                "The gemstone image can only be changed while the listing is Draft or ChangesRequested."
            );
        }

        // Remember the previous image reference.

        var oldImageUrl = listing.PrimaryImageUrl;

        // Save the new image using the registered storage service.

        var newImageUrl =
            await _fileStorageService.SaveGemImageAsync(
                fileStream,
                fileName,
                contentType,
                fileLength
            );

        // Update the database reference.

        listing.PrimaryImageUrl = newImageUrl;

        listing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Database failed:
            // remove the newly uploaded file to avoid an orphan.

            await _fileStorageService.DeleteFileAsync(
                newImageUrl
            );

            throw;
        }

        // Delete previous image only after database success.

        if (
            !string.IsNullOrWhiteSpace(oldImageUrl) &&
            oldImageUrl != newImageUrl
        )
        {
            await _fileStorageService.DeleteFileAsync(
                oldImageUrl
            );
        }

        return await GetListingDtoAsync(listing.Id);
    }

    // ============================================================
    // UPLOAD / REPLACE CERTIFICATE
    //
    // Supported formats depend on IFileStorageService:
    // PDF, JPG, JPEG and PNG.
    // ============================================================

    public async Task<GemListingDto?> UploadCertificateAsync(
        int id,
        Guid sellerId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        var listing = await _context.GemListings
            .FirstOrDefaultAsync(
                g => g.Id == id &&
                     g.SellerId == sellerId
            );

        if (listing == null)
        {
            return null;
        }

        if (
            listing.Status != GemListingStatuses.Draft &&
            listing.Status != GemListingStatuses.ChangesRequested
        )
        {
            throw new InvalidOperationException(
                "The certificate can only be changed while the listing is Draft or ChangesRequested."
            );
        }

        // Remember the existing certificate reference.

        var oldCertificateUrl = listing.CertificateUrl;

        // Upload new certificate.

        var newCertificateUrl =
            await _fileStorageService.SaveCertificateAsync(
                fileStream,
                fileName,
                contentType,
                fileLength
            );

        // Update the certificate reference in PostgreSQL.

        listing.CertificateUrl = newCertificateUrl;

        listing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Remove the new file if saving its URL fails.

            await _fileStorageService.DeleteFileAsync(
                newCertificateUrl
            );

            throw;
        }

        // Delete previous file only after database success.

        if (
            !string.IsNullOrWhiteSpace(oldCertificateUrl) &&
            oldCertificateUrl != newCertificateUrl
        )
        {
            await _fileStorageService.DeleteFileAsync(
                oldCertificateUrl
            );
        }

        return await GetListingDtoAsync(listing.Id);
    }

    // ============================================================
    // INTERNAL DTO MAPPER
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

                PrimaryImageUrl = g.PrimaryImageUrl,

                CertificateNumber = g.CertificateNumber,

                CertificateAuthority = g.CertificateAuthority,

                CertificateUrl = g.CertificateUrl,

                Status = g.Status,

                CreatedAt = g.CreatedAt,

                UpdatedAt = g.UpdatedAt
            })
            .SingleAsync();
    }

    // ============================================================
    // OPTIONAL STRING CLEANER
    // ============================================================

    private static string? CleanOptionalValue(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
