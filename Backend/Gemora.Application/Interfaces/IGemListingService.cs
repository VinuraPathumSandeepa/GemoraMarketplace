using Gemora.Application.DTOs.GemListings;

namespace Gemora.Application.Interfaces;

public interface IGemListingService
{
    // ============================================================
    // CREATE GEM LISTING
    // ============================================================

    Task<GemListingDto> CreateAsync(
        Guid sellerId,
        CreateGemListingDto dto);


    // ============================================================
    // GET CURRENT SELLER'S LISTINGS
    // ============================================================

    Task<List<GemListingDto>> GetMyListingsAsync(
        Guid sellerId);


    // ============================================================
    // GET ONE SELLER LISTING
    // ============================================================

    Task<GemListingDto?> GetByIdAsync(
        int id,
        Guid sellerId);


    // ============================================================
    // UPDATE GEM LISTING
    // ============================================================

    Task<bool> UpdateAsync(
        int id,
        Guid sellerId,
        UpdateGemListingDto dto);


    // ============================================================
    // DELETE GEM LISTING
    // ============================================================

    Task<bool> DeleteAsync(
        int id,
        Guid sellerId);


    // ============================================================
    // SUBMIT LISTING FOR GEMOLOGIST VERIFICATION
    // ============================================================

    Task<GemListingDto?> SubmitForVerificationAsync(
        int id,
        Guid sellerId);


    // ============================================================
    // UPLOAD / REPLACE PRIMARY GEM IMAGE
    //
    // The API controller receives IFormFile and passes only
    // standard .NET values into the Application layer.
    // ============================================================

    Task<GemListingDto?> UploadGemImageAsync(
        int id,
        Guid sellerId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength);


    // ============================================================
    // UPLOAD / REPLACE GEM CERTIFICATE
    // ============================================================

    Task<GemListingDto?> UploadCertificateAsync(
        int id,
        Guid sellerId,
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength);
}