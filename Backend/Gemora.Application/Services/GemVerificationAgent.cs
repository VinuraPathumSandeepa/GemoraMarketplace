using System.Text.Json;

using Gemora.Application.DTOs.GemAI;
using Gemora.Application.Interfaces;
using Gemora.Domain.AI;
using Gemora.Domain.Interfaces;
using Gemora.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Gemora.Application.Services;

public class GemVerificationAgent : IGemVerificationAgent
{
    private readonly ApplicationDbContext _context;
    private readonly IGemEvidenceValidator _evidenceValidator;
    private readonly IGemAiModelClient _aiModelClient;
    private readonly IGemImageReader _gemImageReader;
    private readonly ILogger<GemVerificationAgent> _logger;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public GemVerificationAgent(
        ApplicationDbContext context,
        IGemEvidenceValidator evidenceValidator,
        IGemAiModelClient aiModelClient,
        IGemImageReader gemImageReader,
        ILogger<GemVerificationAgent> logger)
    {
        _context = context;
        _evidenceValidator = evidenceValidator;
        _aiModelClient = aiModelClient;
        _gemImageReader = gemImageReader;
        _logger = logger;
    }


    // ============================================================
    // ANALYZE VERIFICATION
    // ============================================================

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

        if (_context.Database.IsRelational())
        {
            var claimed = await _context.GemVerifications
                .Where(v => v.Id == verificationId && v.Decision == "Pending" && v.AiStatus != "Processing")
                .ExecuteUpdateAsync(update => update.SetProperty(v => v.AiStatus, "Processing"));
            if (claimed != 1)
                throw new InvalidOperationException("AI analysis is already running or this verification is no longer pending.");

            // ExecuteUpdate bypasses tracked entities. Align the original value
            // so a later Completed/Failed status is persisted even when it
            // matches the value loaded before the atomic claim.
            var statusProperty = _context.Entry(verification).Property(v => v.AiStatus);
            statusProperty.OriginalValue = "Processing";
            statusProperty.CurrentValue = "Processing";
        }
        else
        {
            verification.AiStatus = "Processing";
            await _context.SaveChangesAsync();
        }


        var result =
            new GemAiAnalysisResultDto();

        result.WorkflowSteps = new[]
        {
            "load", "state", "validation", "image", "ai", "findings", "review"
        }.Select(id => new GemAiWorkflowStepDto { Id = id }).ToList();

        SetStep(result, "load", "Completed");
        SetStep(result, "state", "Completed");


        result.StepsCompleted.Add(
            "Verification and listing loaded.");


        result.StepsCompleted.Add(
            "Verification workflow state checked.");


        try
        {
            SetStep(result, "validation", "Running");
            // ====================================================
            // STEP 5 — DETERMINISTIC EVIDENCE VALIDATION
            // ====================================================

            var validation =
                _evidenceValidator.Validate(
                    listing);


            result.StepsCompleted.AddRange(
                validation.ChecksPerformed);


            result.ValidationIssues.AddRange(
                validation.Issues);

            result.EvidenceWarnings.AddRange(
                validation.Warnings);


            result.RiskFlags.AddRange(
                validation.Warnings);


            result.StepsCompleted.Add(
                "Deterministic evidence validation completed.");

            SetStep(result, "validation", "Completed");


            // ====================================================
            // STEP 6 — STOP IF REQUIRED EVIDENCE IS INVALID
            // ====================================================

            if (!validation.IsValid)
            {
                SkipPendingSteps(result);
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
                    "Human Gemologist review is still available.";


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
            // ====================================================

            GemImageReadResult? imageEvidence =
                null;


            try
            {
                SetStep(result, "image", "Running");
                imageEvidence =
                    await _gemImageReader.OpenImageAsync(
                        listing.PrimaryImageUrl);


                if (imageEvidence != null)
                {
                    SetStep(result, "image", "Completed");
                    result.StepsCompleted.Add(
                        "Uploaded gemstone image loaded for multimodal analysis.");
                }
                else
                {
                    SetStep(result, "image", "Skipped");
                    result.RiskFlags.Add(
                        "The gemstone image could not be loaded for visual AI analysis.");


                    result.StepsCompleted.Add(
                        "Gemstone image was unavailable; AI will use listing metadata only.");
                }


                // =================================================
                // STEP 8 — START AI MODEL ANALYSIS
                // =================================================

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


                // =================================================
                // STEP 9 — CALL EXTERNAL AI MODEL SAFELY
                //
                // Provider failure should NOT become a fatal
                // Gemora failure.
                //
                // Human Gemologist review remains available.
                // =================================================

                var modelCompleted = false;
                try
                {
                    SetStep(result, "ai", "Running");
                    var modelResult =
                        await _aiModelClient.AnalyzeAsync(
                            listing,
                            imageEvidence?.Stream,
                            imageEvidence?.ContentType);
                    modelCompleted = true;


                    // =============================================
                    // MERGE SUCCESSFUL MODEL RESULT
                    // =============================================

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

                    SetStep(result, "ai", "Completed");
                    SetStep(result, "findings", "Running");


                    // =============================================
                    // VISUAL OBSERVATIONS
                    // =============================================

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


                    // =============================================
                    // MERGE RISK FLAGS
                    // =============================================

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


                    // =============================================
                    // AUDIT STEPS
                    // =============================================

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

                    SetStep(result, "findings", "Completed");
                    SetStep(result, "review", "Completed");


                    // =============================================
                    // PERSIST COMPLETED RESULT
                    // =============================================

                    PersistResult(
                        verification,
                        result,
                        "Completed");


                    await _context.SaveChangesAsync();


                    return result;
                }
                catch (Exception ex)
                {
                    // Database and result-merging failures are internal errors,
                    // never evidence that the provider failed.
                    if (modelCompleted)
                        throw;

                    SetStep(result, "ai", "Failed");
                    SkipPendingSteps(result);

                    if (ex is GemAiProviderException providerError)
                    {
                        result.ErrorCategory = providerError.Category;
                        result.RetryAfterSeconds = providerError.RetryAfterSeconds;
                    }
                    else if (ex is TimeoutException or TaskCanceledException)
                    {
                        result.ErrorCategory = "Timeout";
                    }
                    else
                    {
                        result.ErrorCategory = "ProviderError";
                    }
                    // =============================================
                    // EXTERNAL AI PROVIDER FAILURE
                    //
                    // Examples:
                    // - quota exhausted
                    // - rate limit
                    // - provider unavailable
                    // - invalid API key
                    // - unavailable model
                    //
                    // Return a controlled result instead of HTTP 500.
                    // =============================================

                    _logger.LogWarning(
                        ex,
                        "External AI model failed for verification {VerificationId}. Human review remains available.",
                        verificationId);


                    result.Status =
                        "Failed";


                    result.SuggestedGemType =
                        null;


                    result.ConfidenceScore =
                        null;


                    result.ImageAnalyzed =
                        false;


                    result.Findings =
                        GetSafeAiFailureMessage(
                            ex);


                    const string aiUnavailableRisk =
                        "AI-assisted analysis is currently unavailable. Human Gemologist review is required.";


                    if (!result.RiskFlags.Contains(
                            aiUnavailableRisk,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        result.RiskFlags.Add(
                            aiUnavailableRisk);
                    }


                    result.StepsCompleted.Add(
                        "External AI model request could not be completed.");


                    result.StepsCompleted.Add(
                        "AI failure was handled safely without blocking human Gemologist review.");


                    PersistResult(
                        verification,
                        result,
                        "Failed");


                    await _context.SaveChangesAsync();


                    return result;
                }
            }
            finally
            {
                // =================================================
                // ALWAYS CLOSE IMAGE STREAM
                // =================================================

                if (imageEvidence != null)
                {
                    await imageEvidence.DisposeAsync();
                }
            }
        }
        catch (Exception ex)
        {
            foreach (var step in result.WorkflowSteps.Where(s => s.Status == "Running"))
                step.Status = "Failed";
            if (result.WorkflowSteps.First(s => s.Id == "review").Status == "Completed")
                SetStep(result, "review", "Failed");
            SkipPendingSteps(result);
            // ====================================================
            // STEP 10 — UNEXPECTED INTERNAL GEMORA FAILURE
            //
            // External provider failures are handled above.
            // If execution reaches here, this is a real internal
            // application/database failure.
            // ====================================================

            result.Status = "Failed";
            result.ErrorCategory = "InternalError";
            result.ConfidenceScore = null;
            result.SuggestedGemType = null;
            result.ImageAnalyzed = false;
            result.VisualObservations.Clear();
            result.Findings = "AI analysis could not be saved. Human Gemologist review remains available.";
            PersistResult(verification, result, "Failed");


            _logger.LogError(
                ex,
                "Unexpected AI verification failure for verification {VerificationId}.",
                verificationId);


            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception saveException)
            {
                _logger.LogError(
                    saveException,
                    "Could not persist AI failure state for verification {VerificationId}.",
                    verificationId);
            }


            throw;
        }
    }


    // ============================================================
    // SAFE AI FAILURE MESSAGE
    //
    // Raw provider details stay in backend logs.
    // ============================================================

    private static string GetSafeAiFailureMessage(
        Exception exception)
    {
        if (exception is GemAiProviderException providerError)
        {
            return providerError.Category switch
            {
                "QuotaExceeded" => "The AI provider quota is exhausted. Contact the service administrator before retrying. Human Gemologist review remains available.",
                "UsageLimit" => "The AI provider reported a usage limit. The administrator should check rate limits, quota, and billing before retrying. Human Gemologist review remains available.",
                "RateLimited" => providerError.RetryAfterSeconds is > 0
                    ? $"The AI provider is rate limiting requests. Please wait at least {providerError.RetryAfterSeconds} seconds before retrying. Human Gemologist review remains available."
                    : "The AI provider is rate limiting requests. Please wait before retrying. Human Gemologist review remains available.",
                "Authentication" => "The AI provider credentials require administrator attention. Human Gemologist review remains available.",
                "InvalidRequest" => "The AI provider rejected the analysis request. The service administrator should check model compatibility and configuration. Human Gemologist review remains available.",
                "MalformedResponse" => "The AI provider did not return valid analysis findings. Human Gemologist review remains available.",
                "ModelUnavailable" => "The configured AI model is unavailable. Human Gemologist review remains available.",
                "Timeout" => "The AI provider timed out. Please retry later. Human Gemologist review remains available.",
                _ => "AI-assisted analysis could not be completed. Human Gemologist review remains available."
            };
        }
        var message =
            exception.ToString();


        if (ContainsAny(
                message,
                "429",
                "RESOURCE_EXHAUSTED",
                "quota",
                "rate limit",
                "too many requests"))
        {
            return
                "The AI provider usage limit has been reached temporarily. " +
                "Please retry the AI analysis later. " +
                "The Gemologist can continue with professional manual review.";
        }


        if (ContainsAny(
                message,
                "401",
                "403",
                "API key",
                "unauthorized",
                "forbidden",
                "authentication"))
        {
            return
                "The AI analysis provider configuration requires attention. " +
                "The Gemologist can continue with professional manual review.";
        }


        if (ContainsAny(
                message,
                "404",
                "model not found",
                "model is not found",
                "unsupported model"))
        {
            return
                "The configured AI model is currently unavailable. " +
                "The Gemologist can continue with professional manual review.";
        }


        if (ContainsAny(
                message,
                "timeout",
                "timed out",
                "503",
                "service unavailable",
                "temporarily unavailable"))
        {
            return
                "The AI provider is temporarily unavailable or took too long to respond. " +
                "Please retry later. " +
                "The Gemologist can continue with professional manual review.";
        }


        return
            "AI-assisted analysis could not be completed at this time. " +
            "Please retry later. " +
            "The Gemologist can continue with professional manual review.";
    }


    // ============================================================
    // MESSAGE MATCHING
    // ============================================================

    private static bool ContainsAny(
        string source,
        params string[] values)
    {
        foreach (var value in values)
        {
            if (source.Contains(
                    value,
                    StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }


        return false;
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
        // STORE STRUCTURED AI SUPPORTING INFORMATION
        // ========================================================

        verification.AiRiskFlags =
            JsonSerializer.Serialize(
                new
                {
                    imageAnalyzed =
                        result.ImageAnalyzed,

                    visualObservations =
                        result.VisualObservations,

                    evidenceWarnings =
                        result.EvidenceWarnings,

                    riskFlags =
                        result.RiskFlags,

                    validationIssues =
                        result.ValidationIssues,

                    stepsCompleted =
                        result.StepsCompleted,

                    workflowSteps = result.WorkflowSteps,
                    errorCategory = result.ErrorCategory,
                    retryAfterSeconds = result.RetryAfterSeconds
                });


        verification.AiProcessedAt =
            DateTime.UtcNow;
    }

    private static void SetStep(GemAiAnalysisResultDto result, string id, string status)
    {
        var step = result.WorkflowSteps.First(s => s.Id == id);
        step.Status = status;
    }

    private static void SkipPendingSteps(GemAiAnalysisResultDto result)
    {
        foreach (var step in result.WorkflowSteps.Where(s => s.Status == "Pending"))
            step.Status = "Skipped";
    }
}
