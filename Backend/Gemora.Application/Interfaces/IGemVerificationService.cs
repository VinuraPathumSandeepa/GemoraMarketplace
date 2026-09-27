using Gemora.Application.DTOs.GemVerifications;

namespace Gemora.Application.Interfaces;

public interface IGemVerificationService
{
    Task<List<GemVerificationDto>>
        GetPendingVerificationsAsync();

    Task<GemVerificationDto?>
        GetVerificationByIdAsync(
            int verificationId);

    Task<GemVerificationDto?>
        ReviewVerificationAsync(
            int verificationId,
            Guid gemologistId,
            ReviewGemVerificationDto dto);
}