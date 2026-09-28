using System.Text.Json;
using Gemora.Application.DTOs.GemAI;
using Gemora.Application.Interfaces;
using Gemora.Domain.AI;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemVerificationAgent : IGemVerificationAgent
{
    private readonly ApplicationDbContext _context;
    private readonly IGemEvidenceValidator _evidenceValidator;
    private readonly IGemAiModelClient _aiModelClient;
    private readonly IGemImageReader _gemImageReader;


    public GemVerificationAgent(
        ApplicationDbContext context,
        IGemEvidenceValidator evidenceValidator,
        IGemAiModelClient aiModelClient,
        IGemImageReader gemImageReader)
    {
        _context = context;
        _evidenceValidator = evidenceValidator;
        _aiModelClient = aiModelClient;
        _gemImageReader = gemImageReader;
    }


    public async Task<GemAiAnalysisResultDto> AnalyzeAsync(
        int verificationId)
    {
        // ========================================================
        // STEP 1 — LOAD VERIFICATION + LISTING
        // ========================================================

        var verification =
            await _context.GemVerifications
                .Include(v => v.GemListing)
                .FirstOrDefaultAsync(
                    v => v.Id == verificationId);


        if (verification == null)
        {
            throw new KeyNotFoundException(
                "Gem verification was not found.");
        }


        var listing =
            verification.GemListing;


        if (listing == null)
        {
            throw new InvalidOperationException(
                "The gemstone listing associated with this verification could not be found.");
        }


        // ========================================================
        // STEP 2 — CHECK HUMAN REVIEW STATE
        //
        // AI can only assist while the human decision is Pending.
        // ========================================================

        if (!string.Equals(
                verification.Decision,
                "Pending",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AI analysis can only run for a pending verification.");
        }


        // ========================================================
        // STEP 3 — PREVENT CONCURRENT AI EXECUTION
        // ========================================================

        if (string.Equals(
                verification.AiStatus,
                "Processing",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "AI analysis is already running for this verification.");
        }


        // ========================================================
        // STEP 4 — MARK AGENT AS PROCESSING
        // ========================================================

        verification.AiStatus =
            "Processing";


        await _context.SaveChangesAsync();


        var result =
            new GemAiAnalysisResultDto();


        result.StepsCompleted.Add(
            "Verification and listing loaded.");


        result.StepsCompleted.Add(
            "Verification workflow state checked.");


        try
        {
            // ====================================================
            // STEP 5 — DETERMINISTIC EVIDENCE VALIDATION
            //
            // This runs BEFORE Gemini.
            // ====================================================

            var validation =
                _evidenceValidator.Validate(
                    listing);


            result.StepsCompleted.AddRange(
                validation.ChecksPerformed);


            result.ValidationIssues.AddRange(
                validation.Issues);


            result.RiskFlags.AddRange(
                validation.Warnings);


            result.StepsCompleted.Add(
                "Deterministic evidence validation completed.");


            // ====================================================
            // STEP 6 — SAFE STOP IF REQUIRED EVIDENCE IS INVALID
            // ====================================================

            if (!validation.IsValid)
            {
                result.Status =
                    "NeedsMoreEvidence";


                result.SuggestedGemType =
                    null;


                result.ConfidenceScore =
                    null;


                result.ImageAnalyzed =
                    false;


                result.Findings =
                    "Required evidence validation failed. " +
                    "AI model analysis was not performed. " +
                    "Human review is required.";


                result.StepsCompleted.Add(
                    "AI model analysis skipped because deterministic validation failed.");


                PersistResult(
                    verification,
                    result,
                    "NeedsMoreEvidence");


                await _context.SaveChangesAsync();


                return result;
            }


            // ====================================================
            // STEP 7 — LOAD TRUSTED GEMSTONE IMAGE
            //
            // The Application layer does not know:
            //
            // - wwwroot
            // - physical server paths
            // - IWebHostEnvironment
            //
            // Infrastructure handles those details through
            // IGemImageReader.
            // ====================================================

            GemImageReadResult? imageEvidence =
                null;


            try
            {
                imageEvidence =
                    await _gemImageReader.OpenImageAsync(
                        listing.PrimaryImageUrl);


                if (imageEvidence != null)
                {
                    result.StepsCompleted.Add(
                        "Uploaded gemstone image loaded for multimodal analysis.");
                }
                else
                {
                    result.RiskFlags.Add(
                        "The gemstone image could not be loaded for visual AI analysis.");


                    result.StepsCompleted.Add(
                        "Gemstone image was unavailable; AI will use listing metadata only.");
                }


                // ================================================
                // STEP 8 — CALL ALLOW-LISTED AI MODEL TOOL
                // ================================================

                if (imageEvidence != null)
                {
                    result.StepsCompleted.Add(
                        "Multimodal AI analysis started with listing metadata and gemstone image.");
                }
                else
                {
                    result.StepsCompleted.Add(
                        "AI metadata analysis started.");
                }


                var modelResult =
                    await _aiModelClient.AnalyzeAsync(
                        listing,
                        imageEvidence?.Stream,
                        imageEvidence?.ContentType);


                // ================================================
                // STEP 9 — MERGE AI MODEL RESULT
                // ================================================

                result.Status =
                    "Completed";


                result.SuggestedGemType =
                    modelResult.SuggestedGemType;


                result.ConfidenceScore =
                    modelResult.ConfidenceScore;


                result.Findings =
                    modelResult.Findings;


                result.ImageAnalyzed =
                    modelResult.ImageAnalyzed;


                // ================================================
                // VISUAL OBSERVATIONS
                // ================================================

                if (modelResult.VisualObservations != null)
                {
                    foreach (var observation in
                             modelResult.VisualObservations)
                    {
                        if (string.IsNullOrWhiteSpace(
                                observation))
                        {
                            continue;
                        }


                        if (!result.VisualObservations.Contains(
                                observation,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            result.VisualObservations.Add(
                                observation);
                        }
                    }
                }


                // ================================================
                // MERGE RISK FLAGS
                //
                // Keep deterministic warnings + Gemini flags.
                // Avoid duplicates.
                // ================================================

                if (modelResult.RiskFlags != null)
                {
                    foreach (var riskFlag in
                             modelResult.RiskFlags)
                    {
                        if (string.IsNullOrWhiteSpace(
                                riskFlag))
                        {
                            continue;
                        }


                        if (!result.RiskFlags.Contains(
                                riskFlag,
                                StringComparer.OrdinalIgnoreCase))
                        {
                            result.RiskFlags.Add(
                                riskFlag);
                        }
                    }
                }


                // ================================================
                // AUDIT STEPS
                // ================================================

                if (modelResult.ImageAnalyzed)
                {
                    result.StepsCompleted.Add(
                        "Gemstone image and listing metadata analyzed by the AI model.");
                }
                else
                {
                    result.StepsCompleted.Add(
                        "Listing metadata analyzed by the AI model.");
                }


                result.StepsCompleted.Add(
                    "AI findings prepared for human Gemologist review.");


                // ================================================
                // STEP 10 — PERSIST COMPLETED AI RESULT
                // ================================================

                PersistResult(
                    verification,
                    result,
                    "Completed");


                await _context.SaveChangesAsync();


                return result;
            }
            finally
            {
                // ================================================
                // ALWAYS CLOSE IMAGE STREAM
                // ================================================

                if (imageEvidence != null)
                {
                    await imageEvidence.DisposeAsync();
                }
            }
        }
        catch
        {
            // ====================================================
            // STEP 11 — SAFE FAILURE
            //
            // Never leave:
            //
            // AiStatus = Processing
            //
            // when Gemini/file processing fails.
            // ====================================================

            verification.AiStatus =
                "Failed";


            verification.AiProcessedAt =
                DateTime.UtcNow;


            try
            {
                await _context.SaveChangesAsync();
            }
            catch
            {
                // Preserve the original exception.
            }


            throw;
        }
    }


    // ============================================================
    // PERSIST STRUCTURED AI RESULT
    // ============================================================

    private static void PersistResult(
        Gemora.Domain.Entities.GemVerification verification,
        GemAiAnalysisResultDto result,
        string aiStatus)
    {
        verification.AiStatus =
            aiStatus;


        verification.AiSuggestedGemType =
            result.SuggestedGemType;


        verification.AiConfidenceScore =
            result.ConfidenceScore;


        verification.AiFindings =
            result.Findings;


        // ========================================================
        // Store structured supporting information as JSON.
        //
        // Current DB field:
        //
        // AiRiskFlags
        //
        // For this assignment version it also stores:
        //
        // - imageAnalyzed
        // - visualObservations
        // - riskFlags
        // - validationIssues
        // - stepsCompleted
        //
        // This provides basic persistent AI execution/audit state.
        // ========================================================

        verification.AiRiskFlags =
            JsonSerializer.Serialize(
                new
                {
                    imageAnalyzed =
                        result.ImageAnalyzed,

                    visualObservations =
                        result.VisualObservations,

                    riskFlags =
                        result.RiskFlags,

                    validationIssues =
                        result.ValidationIssues,

                    stepsCompleted =
                        result.StepsCompleted
                });


        verification.AiProcessedAt =
            DateTime.UtcNow;
    }
}