using Microsoft.AspNetCore.Http;

namespace Gemora.API.Services;

public interface IProfileImageStorageService
{
    Task<string> SaveAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken = default);
}