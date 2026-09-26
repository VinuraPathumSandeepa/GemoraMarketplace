using Gemora.Application.DTOs.GemVerifications;
using Gemora.Application.Interfaces;
using Gemora.Domain.Constants;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemVerificationService : IGemVerificationService
{
    private readonly ApplicationDbContext _context;

    public GemVerificationService(
        ApplicationDbContext context)
    {
        _context = context;
    }


    // ============================================================
    // GET PENDING VERIFICATION QUEUE
    // ============================================================

    public async Task<List<GemVerificationDto>>
        GetPendingVerificationsAsync()
    {
        return await _context.GemVerifications
            .AsNoTracking()

            // Only requests that are still waiting for
            // a Gemologist decision.
            .Where(v => v.Decision == "Pending")

            // Oldest verification requests first.
            .OrderBy(v => v.CreatedAt)

            .Select(v => new GemVerificationDto
            {
                // ------------------------------------------------
                // VERIFICATION INFORMATION
                // ------------------------------------------------

                VerificationId = v.Id,

                Decision = v.Decision,

                ReviewNotes = v.ReviewNotes,

                CreatedAt = v.CreatedAt,

                ReviewedAt = v.ReviewedAt,


                // ------------------------------------------------
                // GEM LISTING INFORMATION
                // ------------------------------------------------

                GemListingId = v.GemListingId,

                SellerId = v.GemListing.SellerId,

                SellerName =
                    v.GemListing.Seller.FullName,

                Title =
                    v.GemListing.Title,

                GemType =
                    v.GemListing.GemType,

                Description =
                    v.GemListing.Description,

                CaratWeight =
                    v.GemListing.CaratWeight,

                Color =
                    v.GemListing.Color,

                Clarity =
                    v.GemListing.Clarity,

                Cut =
                    v.GemListing.Cut,

                Price =
                    v.GemListing.Price,

                Currency =
                    v.GemListing.Currency,

                ListingStatus =
                    v.GemListing.Status,


                // ------------------------------------------------
                // AI INFORMATION
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

            .Where(v =>
                v.Id == verificationId)

            .Select(v => new GemVerificationDto
            {
                // ------------------------------------------------
                // VERIFICATION INFORMATION
                // ------------------------------------------------

                VerificationId =
                    v.Id,

                Decision =
                    v.Decision,

                ReviewNotes =
                    v.ReviewNotes,

                CreatedAt =
                    v.CreatedAt,

                ReviewedAt =
                    v.ReviewedAt,


                // ------------------------------------------------
                // GEM LISTING INFORMATION
                // ------------------------------------------------

                GemListingId =
                    v.GemListingId,

                SellerId =
                    v.GemListing.SellerId,

                SellerName =
                    v.GemListing.Seller.FullName,

                Title =
                    v.GemListing.Title,

                GemType =
                    v.GemListing.GemType,

                Description =
                    v.GemListing.Description,

                CaratWeight =
                    v.GemListing.CaratWeight,

                Color =
                    v.GemListing.Color,

                Clarity =
                    v.GemListing.Clarity,

                Cut =
                    v.GemListing.Cut,

                Price =
                    v.GemListing.Price,

                Currency =
                    v.GemListing.Currency,

                ListingStatus =
                    v.GemListing.Status,


                // ------------------------------------------------
                // AI INFORMATION
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
    // REVIEW GEM VERIFICATION
    //
    // A Gemologist can:
    //
    // 1. Approve
    // 2. Request Changes
    // 3. Reject
    //
    // This operation updates both:
    //
    // GemVerification
    // GemListing
    // ============================================================

    public async Task<GemVerificationDto?>
        ReviewVerificationAsync(
            int verificationId,
            Guid gemologistId,
            ReviewGemVerificationDto dto)
    {
        // --------------------------------------------------------
        // STEP 1
        // Verify that the authenticated user is a Gemologist.
        // --------------------------------------------------------

        var gemologistExists =
            await _context.Users.AnyAsync(
                u =>
                    u.Id == gemologistId &&
                    u.Role == UserRoles.Gemologist);

        if (!gemologistExists)
        {
            throw new UnauthorizedAccessException(
                "Only Gemologists can review gem verifications.");
        }


        // --------------------------------------------------------
        // STEP 2
        // Find the verification and its GemListing.
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
        // STEP 3
        // Prevent the same verification from being reviewed twice.
        // --------------------------------------------------------

        if (!string.Equals(
                verification.Decision,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "This verification has already been reviewed.");
        }


        // --------------------------------------------------------
        // STEP 4
        // Make sure the related listing is actually waiting
        // for verification.
        // --------------------------------------------------------

        if (verification.GemListing.Status !=
            GemListingStatuses.PendingVerification)
        {
            throw new InvalidOperationException(
                "The gem listing is not pending verification.");
        }


        // --------------------------------------------------------
        // STEP 5
        // Validate the decision received from the client.
        // --------------------------------------------------------

        var requestedDecision =
            dto.Decision.Trim();

        string normalizedDecision;

        if (requestedDecision.Equals(
                GemListingStatuses.Approved,
                StringComparison.OrdinalIgnoreCase))
        {
            normalizedDecision =
                GemListingStatuses.Approved;
        }
        else if (requestedDecision.Equals(
                     GemListingStatuses.ChangesRequested,
                     StringComparison.OrdinalIgnoreCase))
        {
            normalizedDecision =
                GemListingStatuses.ChangesRequested;
        }
        else if (requestedDecision.Equals(
                     GemListingStatuses.Rejected,
                     StringComparison.OrdinalIgnoreCase))
        {
            normalizedDecision =
                GemListingStatuses.Rejected;
        }
        else
        {
            throw new InvalidOperationException(
                "Decision must be Approved, ChangesRequested, or Rejected.");
        }


        // --------------------------------------------------------
        // STEP 6
        // Notes are mandatory when:
        //
        // - requesting changes
        // - rejecting the listing
        //
        // Approval notes are optional.
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
        // STEP 7
        // Update the GemVerification record.
        // --------------------------------------------------------

        verification.GemologistId =
            gemologistId;

        verification.Decision =
            normalizedDecision;

        verification.ReviewNotes =
            string.IsNullOrWhiteSpace(dto.ReviewNotes)
                ? null
                : dto.ReviewNotes.Trim();

        verification.ReviewedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // STEP 8
        // Synchronize GemListing status with human decision.
        // --------------------------------------------------------

        verification.GemListing.Status =
            normalizedDecision;

        verification.GemListing.UpdatedAt =
            DateTime.UtcNow;


        // --------------------------------------------------------
        // STEP 9
        // Save verification + listing changes together.
        // --------------------------------------------------------

        await _context.SaveChangesAsync();


        // --------------------------------------------------------
        // STEP 10
        // Return the completed verification.
        // --------------------------------------------------------

        return await GetVerificationByIdAsync(
            verificationId);
    }
}