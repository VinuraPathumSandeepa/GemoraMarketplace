using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Services;

public class LocalFileStorageService : IFileStorageService
{
    // ============================================================
    // FILE SIZE LIMITS
    // ============================================================

    private const long MaxGemImageSize =
        5 * 1024 * 1024; // 5 MB

    private const long MaxCertificateSize =
        10 * 1024 * 1024; // 10 MB;


    // ============================================================
    // ALLOWED GEM IMAGE TYPES
    // ============================================================

    private static readonly HashSet<string>
        AllowedImageExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".jpg",
                ".jpeg",
                ".png",
                ".webp"
            };

    private static readonly HashSet<string>
        AllowedImageContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };


    // ============================================================
    // ALLOWED CERTIFICATE TYPES
    // ============================================================

    private static readonly HashSet<string>
        AllowedCertificateExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".pdf",
                ".jpg",
                ".jpeg",
                ".png"
            };

    private static readonly HashSet<string>
        AllowedCertificateContentTypes =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "application/pdf",
                "image/jpeg",
                "image/png"
            };


    // ============================================================
    // UPLOAD ROOT
    //
    // This value is supplied by Gemora.API Program.cs.
    //
    // Example:
    //
    // C:\...\Gemora.API\wwwroot\uploads
    // ============================================================

    private readonly string _uploadRoot;


    public LocalFileStorageService(
        string uploadRoot)
    {
        if (string.IsNullOrWhiteSpace(uploadRoot))
        {
            throw new ArgumentException(
                "Upload root path cannot be empty.",
                nameof(uploadRoot));
        }

        _uploadRoot =
            Path.GetFullPath(uploadRoot);

        Directory.CreateDirectory(
            _uploadRoot);
    }


    // ============================================================
    // SAVE GEM IMAGE
    // ============================================================

    public async Task<string> SaveGemImageAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        ValidateFile(
            fileName,
            contentType,
            fileLength,
            MaxGemImageSize,
            AllowedImageExtensions,
            AllowedImageContentTypes,
            "gem image");

        return await SaveFileAsync(
            fileStream,
            fileName,
            "gem-images");
    }


    // ============================================================
    // SAVE CERTIFICATE
    // ============================================================

    public async Task<string> SaveCertificateAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long fileLength)
    {
        ValidateFile(
            fileName,
            contentType,
            fileLength,
            MaxCertificateSize,
            AllowedCertificateExtensions,
            AllowedCertificateContentTypes,
            "certificate");

        return await SaveFileAsync(
            fileStream,
            fileName,
            "certificates");
    }


    // ============================================================
    // SAVE PHYSICAL FILE
    // ============================================================

    private async Task<string> SaveFileAsync(
        Stream fileStream,
        string originalFileName,
        string folderName)
    {
        var extension =
            Path.GetExtension(originalFileName)
                .ToLowerInvariant();

        // Never store the original user-provided filename.
        var safeFileName =
            $"{Guid.NewGuid():N}{extension}";

        var directory =
            Path.Combine(
                _uploadRoot,
                folderName);

        Directory.CreateDirectory(
            directory);

        var fullPath =
            Path.Combine(
                directory,
                safeFileName);

        await using var outputStream =
            new FileStream(
                fullPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);

        await fileStream.CopyToAsync(
            outputStream);

        // Only store a relative public URL in PostgreSQL.
        return
            $"/uploads/{folderName}/{safeFileName}";
    }


    // ============================================================
    // DELETE STORED FILE
    // ============================================================

    public Task DeleteFileAsync(
        string? fileUrl)
    {
        if (string.IsNullOrWhiteSpace(
                fileUrl))
        {
            return Task.CompletedTask;
        }

        // Only files created by this application's local upload
        // system may be deleted.
        //
        // External URLs such as:
        // https://example.com/...
        //
        // will simply be ignored.
        if (!fileUrl.StartsWith(
                "/uploads/",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }


        // Example:
        //
        // /uploads/gem-images/test.jpg
        //
        // becomes:
        //
        // gem-images\test.jpg
        var relativeUploadPath =
            fileUrl
                .Substring("/uploads/".Length)
                .Replace(
                    '/',
                    Path.DirectorySeparatorChar);


        var fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _uploadRoot,
                    relativeUploadPath));


        // ========================================================
        // PATH TRAVERSAL PROTECTION
        // ========================================================

        var allowedRoot =
            Path.GetFullPath(
                    _uploadRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar,
                    Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;


        if (!fullPath.StartsWith(
                allowedRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid uploaded file path.");
        }


        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }


        return Task.CompletedTask;
    }


    // ============================================================
    // VALIDATE FILE
    // ============================================================

    private static void ValidateFile(
        string fileName,
        string contentType,
        long fileLength,
        long maximumSize,
        HashSet<string> allowedExtensions,
        HashSet<string> allowedContentTypes,
        string fileDescription)
    {
        // --------------------------------------------------------
        // Empty file
        // --------------------------------------------------------

        if (fileLength <= 0)
        {
            throw new InvalidOperationException(
                $"The {fileDescription} file is empty.");
        }


        // --------------------------------------------------------
        // File size
        // --------------------------------------------------------

        if (fileLength > maximumSize)
        {
            var maximumSizeMb =
                maximumSize /
                (1024 * 1024);

            throw new InvalidOperationException(
                $"The {fileDescription} must not exceed {maximumSizeMb} MB.");
        }


        // --------------------------------------------------------
        // Extension
        // --------------------------------------------------------

        var extension =
            Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(
                extension) ||
            !allowedExtensions.Contains(
                extension))
        {
            throw new InvalidOperationException(
                $"The selected file type is not allowed for the {fileDescription}.");
        }


        // --------------------------------------------------------
        // MIME / Content-Type
        // --------------------------------------------------------

        if (string.IsNullOrWhiteSpace(
                contentType) ||
            !allowedContentTypes.Contains(
                contentType))
        {
            throw new InvalidOperationException(
                $"The selected file has an invalid content type for the {fileDescription}.");
        }
    }
}