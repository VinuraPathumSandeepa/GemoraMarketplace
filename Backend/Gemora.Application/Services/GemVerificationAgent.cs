using System.Text.Json;
using Gemora.Application.DTOs.GemAI;
using Gemora.Application.Interfaces;
using Gemora.Domain.AI;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.Application.Services;

public class GemVerificationAgent : IGemVerificationAgent
{
    private readonly ApplicationDbContext _context;
    private readonly IGemEvidenceValidator _evidenceValidator;
    private readonly IGemAiModelClient _aiModelClient;

    public GemVerificationAgent(
        ApplicationDbContext context,
        IGemEvidenceValidator evidenceValidator,
        IGemAiModelClient aiModelClient)
    {
        _context = context;
        _evidenceValidator = evidenceValidator;
        _aiModelClient = aiModelClient;
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
        // STEP 3 — PREVENT CONCURRENT EXECUTION
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
        // STEP 4 — PERSIST PROCESSING STATE
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
            // STEP 5 — DETERMINISTIC VALIDATION TOOL
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

                result.Findings =
                    "Required evidence validation failed. AI model analysis was not performed. Human review is required.";


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
            // STEP 7 — CALL ALLOW-LISTED AI MODEL TOOL
            //
            // Current implementation:
            //
            // IGemAiModelClient
            //      ↓
            // GeminiGemAnalysisClient
            //      ↓
            // Gemini API
            //
            // The provider remains hidden behind the interface.
            // ====================================================

            result.StepsCompleted.Add(
                "AI model analysis started.");


            var modelResult =
                await _aiModelClient.AnalyzeAsync(
                    listing);


            // ====================================================
            // STEP 8 — MERGE MODEL RESULT
            // ====================================================

            result.Status =
                "Completed";


            result.SuggestedGemType =
                modelResult.SuggestedGemType;


            result.ConfidenceScore =
                modelResult.ConfidenceScore;


            result.Findings =
                modelResult.Findings;


            if (modelResult.RiskFlags != null)
            {
                result.RiskFlags.AddRange(
                    modelResult.RiskFlags);
            }


            result.StepsCompleted.Add(
                "AI model analysis completed.");


            result.StepsCompleted.Add(
                "AI findings prepared for human Gemologist review.");


            // ====================================================
            // STEP 9 — PERSIST COMPLETED AGENT RESULT
            // ====================================================

            PersistResult(
                verification,
                result,
                "Completed");


            await _context.SaveChangesAsync();


            return result;
        }
        catch
        {
            // ====================================================
            // STEP 10 — SAFE FAILURE
            //
            // Never leave the verification permanently stuck in:
            //
            // AiStatus = Processing
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


        // Store the structured audit information as JSON.
        verification.AiRiskFlags =
            JsonSerializer.Serialize(
                new
                {
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