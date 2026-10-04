namespace Gemora.Domain.Interfaces;

public interface IFileStorageService
{
    Task<string> SaveGemImageAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength);

    Task<string> SaveCertificateAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength);

    Task DeleteFileAsync(string? fileUrl);
}