using Gemora.Application.DTOs.GemListings;

namespace Gemora.Application.Interfaces;

public interface IGemListingService
{
    Task<GemListingDto> CreateAsync(
        Guid sellerId,
        CreateGemListingDto dto);

    Task<List<GemListingDto>> GetMyListingsAsync(
        Guid sellerId);

    Task<GemListingDto?> GetByIdAsync(
        int id,
        Guid sellerId);

    Task<bool> UpdateAsync(
        int id,
        Guid sellerId,
        UpdateGemListingDto dto);

    Task<bool> DeleteAsync(
        int id,
        Guid sellerId);

    Task<GemListingDto?> SubmitForVerificationAsync(
    int id,
    Guid sellerId);
}