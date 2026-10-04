using Gemora.Application.DTOs.GemAI;
using Gemora.Domain.Entities;

namespace Gemora.Application.Interfaces;

public interface IGemEvidenceValidator
{
    GemEvidenceValidationResultDto Validate(
        GemListing listing);
}