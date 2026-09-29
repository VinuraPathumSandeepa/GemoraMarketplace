using Microsoft.AspNetCore.Http;

namespace Gemora.API.Services;

public class ProfileImageStorageService :
    IProfileImageStorageService
{
    private const long MaxFileSize =
        5 * 1024 * 1024;

    private readonly IWebHostEnvironment _environment;


    public ProfileImageStorageService(
        IWebHostEnvironment environment)
    {
        _environment = environment;
    }


    // ============================================================
    // SAVE PROFILE IMAGE
    // ============================================================

    public async Task<string> SaveAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null ||
            file.Length <= 0)
        {
            throw new InvalidOperationException(
                "Please select a profile image."
            );
        }


        if (file.Length > MaxFileSize)
        {
            throw new InvalidOperationException(
                "The profile image must be 5 MB or smaller."
            );
        }


        // --------------------------------------------------------
        // Check declared MIME type
        // --------------------------------------------------------

        var allowedContentTypes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };


        if (!allowedContentTypes.Contains(
                file.ContentType))
        {
            throw new InvalidOperationException(
                "Please upload a JPG, PNG, or WebP profile image."
            );
        }


        // --------------------------------------------------------
        // Inspect the actual file signature
        //
        // We do not trust only:
        // - extension
        // - browser Content-Type
        // --------------------------------------------------------

        var extension =
            await DetectImageExtensionAsync(
                file,
                cancellationToken
            );


        if (extension == null)
        {
            throw new InvalidOperationException(
                "The selected file does not appear to be a valid JPG, PNG, or WebP image."
            );
        }


        // --------------------------------------------------------
        // Resolve wwwroot
        // --------------------------------------------------------

        var webRoot =
            _environment.WebRootPath;


        if (string.IsNullOrWhiteSpace(
                webRoot))
        {
            webRoot =
                Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"
                );
        }


        var profileDirectory =
            Path.Combine(
                webRoot,
                "uploads",
                "profiles"
            );


        Directory.CreateDirectory(
            profileDirectory
        );


        // --------------------------------------------------------
        // Generate safe filename
        //
        // Never use the original filename for server storage.
        // --------------------------------------------------------

        var fileName =
            $"{userId:N}_{Guid.NewGuid():N}{extension}";


        var physicalPath =
            Path.Combine(
                profileDirectory,
                fileName
            );


        // --------------------------------------------------------
        // Save file
        // --------------------------------------------------------

        await using (
            var inputStream =
                file.OpenReadStream())
        await using (
            var outputStream =
                new FileStream(
                    physicalPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true
                ))
        {
            await inputStream.CopyToAsync(
                outputStream,
                cancellationToken
            );
        }


        // Store only a web-relative URL in PostgreSQL.

        return
            $"/uploads/profiles/{fileName}";
    }


    // ============================================================
    // DELETE PROFILE IMAGE
    // ============================================================

    public Task DeleteAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                profileImageUrl))
        {
            return Task.CompletedTask;
        }


        /*
         * SECURITY:
         * We delete only files located inside the
         * expected profile-image URL directory.
         */

        if (!profileImageUrl.StartsWith(
                "/uploads/profiles/",
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }


        var fileName =
            Path.GetFileName(
                profileImageUrl
            );


        if (string.IsNullOrWhiteSpace(
                fileName))
        {
            return Task.CompletedTask;
        }


        var webRoot =
            _environment.WebRootPath;


        if (string.IsNullOrWhiteSpace(
                webRoot))
        {
            webRoot =
                Path.Combine(
                    _environment.ContentRootPath,
                    "wwwroot"
                );
        }


        var physicalPath =
            Path.Combine(
                webRoot,
                "uploads",
                "profiles",
                fileName
            );


        if (File.Exists(
                physicalPath))
        {
            File.Delete(
                physicalPath
            );
        }


        return Task.CompletedTask;
    }


    // ============================================================
    // FILE SIGNATURE VALIDATION
    // ============================================================

    private static async Task<string?>
        DetectImageExtensionAsync(
            IFormFile file,
            CancellationToken cancellationToken)
    {
        var header =
            new byte[12];


        await using var stream =
            file.OpenReadStream();


        var bytesRead =
            await stream.ReadAsync(
                header.AsMemory(
                    0,
                    header.Length
                ),
                cancellationToken
            );


        // --------------------------------------------------------
        // JPEG
        //
        // FF D8 FF
        // --------------------------------------------------------

        if (bytesRead >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return ".jpg";
        }


        // --------------------------------------------------------
        // PNG
        //
        // 89 50 4E 47 0D 0A 1A 0A
        // --------------------------------------------------------

        if (bytesRead >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return ".png";
        }


        // --------------------------------------------------------
        // WEBP
        //
        // RIFF .... WEBP
        // --------------------------------------------------------

        if (bytesRead >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P')
        {
            return ".webp";
        }


        return null;
    }
}