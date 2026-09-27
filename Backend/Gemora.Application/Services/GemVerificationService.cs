using Gemora.Application.DTOs.GemVerifications;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemVerificationService : IGemVerificationService
{
    private readonly ApplicationDbContext _context;

    public GemVerificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ============================================================
    // GET ALL PENDING VERIFICATIONS
    // ============================================================

    public async Task<List<GemVerificationDto>>
        GetPendingVerificationsAsync()
    {
        return await _context.GemVerifications
            .AsNoTracking()
            .Where(v => v.Decision == "Pending")
            .OrderBy(v => v.CreatedAt)
            .Select(v => new GemVerificationDto
            {
                // ------------------------------------------------
                // VERIFICATION INFORMATION
                // ------------------------------------------------

                VerificationId = v.Id,

                GemListingId = v.GemListingId,

                Decision = v.Decision,

                ReviewNotes = v.ReviewNotes,

                CreatedAt = v.CreatedAt,

                ReviewedAt = v.ReviewedAt,


                // ------------------------------------------------
                // GEM LISTING INFORMATION
                // ------------------------------------------------

                SellerId = v.GemListing.SellerId,

                SellerName = v.GemListing.Seller.FullName,

                Title = v.GemListing.Title,

                GemType = v.GemListing.GemType,

                Description = v.GemListing.Description,

                CaratWeight = v.GemListing.CaratWeight,

                Color = v.GemListing.Color,

                Clarity = v.GemListing.Clarity,

                Cut = v.GemListing.Cut,

                Price = v.GemListing.Price,

                Currency = v.GemListing.Currency,


                // ------------------------------------------------
                // GEM IMAGE / CERTIFICATE EVIDENCE
                // ------------------------------------------------

                PrimaryImageUrl =
                    v.GemListing.PrimaryImageUrl,

                CertificateNumber =
                    v.GemListing.CertificateNumber,

                CertificateAuthority =
                    v.GemListing.CertificateAuthority,

                CertificateUrl =
                    v.GemListing.CertificateUrl,


                // ------------------------------------------------
                // LISTING WORKFLOW
                // ------------------------------------------------

                ListingStatus =
                    v.GemListing.Status,


                // ------------------------------------------------
                // AI VERIFICATION INFORMATION
                // ------------------------------------------------

                AiStatus =
                    v.AiStatus,

                AiSuggestedGemType =
                    v.AiSuggestedGemType,

                AiConfidenceScore =
                    v.AiConfidenceScore,

                AiFindings =
                    v.AiFindings,

                AiRiskFlags =
                    v.AiRiskFlags,

                AiProcessedAt =
                    v.AiProcessedAt,


                // ------------------------------------------------
                // GEMOLOGIST INFORMATION
                // ------------------------------------------------

                GemologistId =
                    v.GemologistId,

                GemologistName =
                    v.Gemologist != null
                        ? v.Gemologist.FullName
                        : null
            })
            .ToListAsync();
    }


    // ============================================================
    // GET ONE VERIFICATION BY ID
    // ============================================================

    public async Task<GemVerificationDto?>
        GetVerificationByIdAsync(
            int verificationId)
    {
        return await _context.GemVerifications
            .AsNoTracking()
            .Where(v => v.Id == verificationId)
            .Select(v => new GemVerificationDto
            {
                // ------------------------------------------------
                // VERIFICATION INFORMATION
                // ------------------------------------------------

                VerificationId = v.Id,

                GemListingId = v.GemListingId,

                Decision = v.Decision,

                ReviewNotes = v.ReviewNotes,

                CreatedAt = v.CreatedAt,

                ReviewedAt = v.ReviewedAt,


                // ------------------------------------------------
                // GEM LISTING INFORMATION
                // ------------------------------------------------

                SellerId = v.GemListing.SellerId,

                SellerName = v.GemListing.Seller.FullName,

                Title = v.GemListing.Title,

                GemType = v.GemListing.GemType,

                Description = v.GemListing.Description,

                CaratWeight = v.GemListing.CaratWeight,

                Color = v.GemListing.Color,

                Clarity = v.GemListing.Clarity,

                Cut = v.GemListing.Cut,

                Price = v.GemListing.Price,

                Currency = v.GemListing.Currency,


                // ------------------------------------------------
                // GEM IMAGE / CERTIFICATE EVIDENCE
                // ------------------------------------------------

                PrimaryImageUrl =
                    v.GemListing.PrimaryImageUrl,

                CertificateNumber =
                    v.GemListing.CertificateNumber,

                CertificateAuthority =
                    v.GemListing.CertificateAuthority,

                CertificateUrl =
                    v.GemListing.CertificateUrl,


                // ------------------------------------------------
                // LISTING WORKFLOW
                // ------------------------------------------------

                ListingStatus =
                    v.GemListing.Status,


                // ------------------------------------------------
                // AI VERIFICATION INFORMATION
                // ------------------------------------------------

                AiStatus =
                    v.AiStatus,

                AiSuggestedGemType =
                    v.AiSuggestedGemType,

                AiConfidenceScore =
                    v.AiConfidenceScore,

                AiFindings =
                    v.AiFindings,

                AiRiskFlags =
                    v.AiRiskFlags,

                AiProcessedAt =
                    v.AiProcessedAt,


                // ------------------------------------------------
                // GEMOLOGIST INFORMATION
                // ------------------------------------------------

                GemologistId =
                    v.GemologistId,

                GemologistName =
                    v.Gemologist != null
                        ? v.Gemologist.FullName
                        : null
            })
            .FirstOrDefaultAsync();
    }


    // ============================================================
    // REVIEW VERIFICATION
    //
    // Allowed decisions:
    // Approved
    // ChangesRequested
    // Rejected
    // ============================================================

    public async Task<GemVerificationDto?>
        ReviewVerificationAsync(
            int verificationId,
            Guid gemologistId,
            ReviewGemVerificationDto dto)
    {
        // --------------------------------------------------------
        // 1. Confirm current user is a Gemologist
        // --------------------------------------------------------

        var gemologistExists =
            await _context.Users.AnyAsync(
                u =>
                    u.Id == gemologistId &&
                    u.Role == UserRoles.Gemologist);

        if (!gemologistExists)
        {
            throw new UnauthorizedAccessException(
                "Only Gemologists can review gem verification requests.");
        }


        // --------------------------------------------------------
        // 2. Find verification and associated listing
        // --------------------------------------------------------

        var verification =
            await _context.GemVerifications
                .Include(v => v.GemListing)
                .FirstOrDefaultAsync(
                    v => v.Id == verificationId);

        if (verification == null)
        {
            return null;
        }


        // --------------------------------------------------------
        // 3. Prevent duplicate review
        // --------------------------------------------------------

        if (!string.Equals(
                verification.Decision,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "This verification request has already been reviewed.");
        }


        // --------------------------------------------------------
        // 4. Listing must currently be pending verification
        // --------------------------------------------------------

        if (verification.GemListing.Status !=
            GemListingStatuses.PendingVerification)
        {
            throw new InvalidOperationException(
                "This listing is not currently awaiting verification.");
        }


        // --------------------------------------------------------
        // 5. Validate decision
        // --------------------------------------------------------

        var normalizedDecision =
            NormalizeDecision(dto.Decision);

        if (normalizedDecision == null)
        {
            throw new InvalidOperationException(
                "Decision must be Approved, ChangesRequested, or Rejected.");
        }


        // --------------------------------------------------------
        // 6. Require notes for ChangesRequested / Rejected
        // --------------------------------------------------------

        if ((normalizedDecision ==
                GemListingStatuses.ChangesRequested ||
             normalizedDecision ==
                GemListingStatuses.Rejected) &&
            string.IsNullOrWhiteSpace(dto.ReviewNotes))
        {
            throw new InvalidOperationException(
                "Review notes are required when requesting changes or rejecting a listing.");
        }


        // --------------------------------------------------------
        // 7. Save Gemologist review
        // --------------------------------------------------------

        verification.GemologistId =
            gemologistId;

        verification.Decision =
            normalizedDecision;

        verification.ReviewNotes =
            CleanOptionalValue(dto.ReviewNotes);

        verification.ReviewedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // 8. Update listing status
        //
        // PendingVerification
        //        ↓
        // Approved / ChangesRequested / Rejected
        // --------------------------------------------------------

        verification.GemListing.Status =
            normalizedDecision;

        verification.GemListing.UpdatedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // 9. Save changes
        // --------------------------------------------------------

        await _context.SaveChangesAsync();


        // --------------------------------------------------------
        // 10. Return updated verification
        // --------------------------------------------------------

        return await GetVerificationByIdAsync(
            verificationId);
    }


    // ============================================================
    // NORMALIZE DECISION
    // ============================================================

    private static string?
        NormalizeDecision(
            string decision)
    {
        if (string.IsNullOrWhiteSpace(decision))
        {
            return null;
        }

        var value =
            decision.Trim();

        if (value.Equals(
                GemListingStatuses.Approved,
                StringComparison.OrdinalIgnoreCase))
        {
            return GemListingStatuses.Approved;
        }

        if (value.Equals(
                GemListingStatuses.ChangesRequested,
                StringComparison.OrdinalIgnoreCase))
        {
            return GemListingStatuses.ChangesRequested;
        }

        if (value.Equals(
                GemListingStatuses.Rejected,
                StringComparison.OrdinalIgnoreCase))
        {
            return GemListingStatuses.Rejected;
        }

        return null;
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