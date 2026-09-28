using Gemora.Application.DTOs.GemAI;

namespace Gemora.Application.Interfaces;

public interface IGemVerificationAgent
{
    Task<GemAiAnalysisResultDto> AnalyzeAsync(
        int verificationId);
}