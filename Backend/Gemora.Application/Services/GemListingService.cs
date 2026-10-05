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
        var sellerExists =
            await _context.Users.AnyAsync(
                u =>
                    u.Id == sellerId &&
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

            Currency =
                dto.Currency
                    .Trim()
                    .ToUpperInvariant(),

            // ----------------------------------------------------
            // Evidence
            // ----------------------------------------------------

            PrimaryImageUrl =
                CleanOptionalValue(
                    dto.PrimaryImageUrl),

            CertificateNumber =
                CleanOptionalValue(
                    dto.CertificateNumber),

            CertificateAuthority =
                CleanOptionalValue(
                    dto.CertificateAuthority),

            CertificateUrl =
                CleanOptionalValue(
                    dto.CertificateUrl),

            // ----------------------------------------------------
            // Workflow
            // ----------------------------------------------------

            Status =
                GemListingStatuses.Draft,

            CreatedAt =
                DateTime.UtcNow
        };

        _context.GemListings.Add(listing);

        await _context.SaveChangesAsync();

        return await GetListingDtoAsync(
            listing.Id);
    }


    // ============================================================
    // GET CURRENT SELLER'S LISTINGS
    // ============================================================

    public async Task<List<GemListingDto>>
        GetMyListingsAsync(
            Guid sellerId)
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

                Description =
                    g.Description,

                CaratWeight =
                    g.CaratWeight,

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

                CreatedAt =
                    g.CreatedAt,

                UpdatedAt =
                    g.UpdatedAt
            })

            .ToListAsync();
    }


    // ============================================================
    // GET ONE LISTING
    //
    // Ownership is enforced.
    // A seller cannot retrieve another seller's listing here.
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

                Description =
                    g.Description,

                CaratWeight =
                    g.CaratWeight,

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

                CreatedAt =
                    g.CreatedAt,

                UpdatedAt =
                    g.UpdatedAt
            })

            .FirstOrDefaultAsync();
    }


    // ============================================================
    // UPDATE LISTING
    //
    // Only Draft and ChangesRequested listings can be edited.
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

        if (listing.Status !=
                GemListingStatuses.Draft &&
            listing.Status !=
                GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "Only Draft or ChangesRequested listings can be edited.");
        }


        // --------------------------------------------------------
        // Basic gem information
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
        // Evidence metadata
        // --------------------------------------------------------

        listing.PrimaryImageUrl =
            CleanOptionalValue(
                dto.PrimaryImageUrl);

        listing.CertificateNumber =
            CleanOptionalValue(
                dto.CertificateNumber);

        listing.CertificateAuthority =
            CleanOptionalValue(
                dto.CertificateAuthority);

        listing.CertificateUrl =
            CleanOptionalValue(
                dto.CertificateUrl);


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

        if (listing.Status !=
            GemListingStatuses.Draft)
        {
            throw new InvalidOperationException(
                "Only Draft listings can be deleted.");
        }


        // --------------------------------------------------------
        // Remember locally stored evidence so it can be removed
        // after the database operation succeeds.
        // --------------------------------------------------------

        var imageUrl =
            listing.PrimaryImageUrl;

        var certificateUrl =
            listing.CertificateUrl;


        _context.GemListings.Remove(
            listing);

        await _context.SaveChangesAsync();


        // --------------------------------------------------------
        // Clean up locally uploaded files.
        //
        // DeleteFileAsync ignores external URLs, so our old
        // example.com test URLs will not be deleted.
        // --------------------------------------------------------

        await _fileStorageService
            .DeleteFileAsync(imageUrl);

        await _fileStorageService
            .DeleteFileAsync(certificateUrl);

        return true;
    }


    // ============================================================
    // SUBMIT FOR VERIFICATION
    //
    // Draft / ChangesRequested
    //          ↓
    // PendingVerification
    //
    // Every submission creates a NEW GemVerification record,
    // preserving verification history.
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

        if (listing.Status !=
                GemListingStatuses.Draft &&
            listing.Status !=
                GemListingStatuses.ChangesRequested)
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
        // Create a new verification attempt
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
    // UPLOAD / REPLACE PRIMARY GEM IMAGE
    //
    // Only the owner can upload.
    // Only Draft / ChangesRequested listings can be changed.
    // ============================================================

    public async Task<GemListingDto?>
        UploadGemImageAsync(
            int id,
            Guid sellerId,
            Stream fileStream,
            string fileName,
            string contentType,
            long fileLength)
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

        if (listing.Status !=
                GemListingStatuses.Draft &&
            listing.Status !=
                GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "The gemstone image can only be changed while the listing is Draft or ChangesRequested.");
        }


        // --------------------------------------------------------
        // Remember existing image.
        // --------------------------------------------------------

        var oldImageUrl =
            listing.PrimaryImageUrl;


        // --------------------------------------------------------
        // Save new image first.
        //
        // LocalFileStorageService performs file validation.
        // --------------------------------------------------------

        var newImageUrl =
            await _fileStorageService
                .SaveGemImageAsync(
                    fileStream,
                    fileName,
                    contentType,
                    fileLength);


        // --------------------------------------------------------
        // Update database
        // --------------------------------------------------------

        listing.PrimaryImageUrl =
            newImageUrl;

        listing.UpdatedAt =
            DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            // If DB update fails, remove the newly uploaded file
            // so we do not leave an orphaned file on disk.

            await _fileStorageService
                .DeleteFileAsync(
                    newImageUrl);

            throw;
        }


        // --------------------------------------------------------
        // Database update succeeded.
        // We can now safely remove the previous local image.
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                oldImageUrl) &&
            oldImageUrl != newImageUrl)
        {
            await _fileStorageService
                .DeleteFileAsync(
                    oldImageUrl);
        }


        return await GetListingDtoAsync(
            listing.Id);
    }


    // ============================================================
    // UPLOAD / REPLACE CERTIFICATE
    //
    // Supported by storage service:
    // PDF / JPG / JPEG / PNG
    // ============================================================

    public async Task<GemListingDto?>
        UploadCertificateAsync(
            int id,
            Guid sellerId,
            Stream fileStream,
            string fileName,
            string contentType,
            long fileLength)
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

        if (listing.Status !=
                GemListingStatuses.Draft &&
            listing.Status !=
                GemListingStatuses.ChangesRequested)
        {
            throw new InvalidOperationException(
                "The certificate can only be changed while the listing is Draft or ChangesRequested.");
        }


        // --------------------------------------------------------
        // Remember existing certificate.
        // --------------------------------------------------------

        var oldCertificateUrl =
            listing.CertificateUrl;


        // --------------------------------------------------------
        // Save new certificate
        // --------------------------------------------------------

        var newCertificateUrl =
            await _fileStorageService
                .SaveCertificateAsync(
                    fileStream,
                    fileName,
                    contentType,
                    fileLength);


        // --------------------------------------------------------
        // Update database
        // --------------------------------------------------------

        listing.CertificateUrl =
            newCertificateUrl;

        listing.UpdatedAt =
            DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch
        {
            await _fileStorageService
                .DeleteFileAsync(
                    newCertificateUrl);

            throw;
        }


        // --------------------------------------------------------
        // Remove previous local certificate only after DB success.
        // --------------------------------------------------------

        if (!string.IsNullOrWhiteSpace(
                oldCertificateUrl) &&
            oldCertificateUrl !=
                newCertificateUrl)
        {
            await _fileStorageService
                .DeleteFileAsync(
                    oldCertificateUrl);
        }


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

                Description =
                    g.Description,

                CaratWeight =
                    g.CaratWeight,

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

                CreatedAt =
                    g.CreatedAt,

                UpdatedAt =
                    g.UpdatedAt
            })

            .SingleAsync();
    }


    // ============================================================
    // OPTIONAL STRING CLEANER
    // ============================================================

    private static string?
        CleanOptionalValue(
            string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}