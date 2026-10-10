using Gemora.Infrastructure.Services;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Gemora.API.Services;

public sealed class ProfileImageStorageService
    : IProfileImageStorageService
{
    // ============================================================
    // CONSTANTS
    // ============================================================

    private const long MaxFileSize =
        5L * 1024 * 1024;

    private const string SupabaseBucket =
        "profile-images";

    private const string LocalProfileUrlPrefix =
        "/uploads/profiles/";

    private const string SupabaseProfilePath =
        "/storage/v1/object/public/profile-images/";


    // ============================================================
    // DEPENDENCIES
    // ============================================================

    private readonly SupabaseStorageClient
        _supabaseStorage;

    private readonly ILogger<ProfileImageStorageService>
        _logger;

    private readonly IWebHostEnvironment
        _environment;

    private readonly string
        _webRootPath;


    // ============================================================
    // CONSTRUCTOR
    // ============================================================

    public ProfileImageStorageService(
        SupabaseStorageClient supabaseStorage,
        IWebHostEnvironment environment,
        ILogger<ProfileImageStorageService> logger)
    {
        _supabaseStorage =
            supabaseStorage;

        _environment =
            environment;

        _logger =
            logger;


        var configuredWebRoot =
            environment.WebRootPath;


        if (
            string.IsNullOrWhiteSpace(
                configuredWebRoot)
        )
        {
            configuredWebRoot =
                Path.Combine(
                    environment.ContentRootPath,
                    "wwwroot"
                );
        }


        _webRootPath =
            Path.GetFullPath(
                configuredWebRoot
            );
    }


    // ============================================================
    // SAVE PROFILE IMAGE
    // ============================================================

    public async Task<string> SaveAsync(
        Guid userId,
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();


        // --------------------------------------------------------
        // VALIDATE FILE
        // --------------------------------------------------------

        if (
            file == null ||
            file.Length <= 0
        )
        {
            throw new InvalidOperationException(
                "Please select a profile image."
            );
        }


        if (
            file.Length >
            MaxFileSize
        )
        {
            throw new InvalidOperationException(
                "The profile image must be 5 MB or smaller."
            );
        }


        // --------------------------------------------------------
        // VALIDATE CONTENT TYPE
        // --------------------------------------------------------

        var allowedContentTypes =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg",
                "image/png",
                "image/webp"
            };


        if (
            !allowedContentTypes.Contains(
                file.ContentType)
        )
        {
            throw new InvalidOperationException(
                "Please upload a JPG, PNG, or WebP profile image."
            );
        }


        // --------------------------------------------------------
        // DETECT ACTUAL IMAGE FORMAT
        // --------------------------------------------------------

        var extension =
            await DetectImageExtensionAsync(
                file,
                cancellationToken
            );


        if (
            extension == null
        )
        {
            throw new InvalidOperationException(
                "The selected file does not appear to be a valid JPG, PNG, or WebP image."
            );
        }


        // --------------------------------------------------------
        // VERIFY CONTENT TYPE MATCHES FILE SIGNATURE
        // --------------------------------------------------------

        var expectedContentType =
            extension switch
            {
                ".jpg" =>
                    "image/jpeg",

                ".png" =>
                    "image/png",

                ".webp" =>
                    "image/webp",

                _ =>
                    null
            };


        if (
            expectedContentType == null ||
            !string.Equals(
                file.ContentType,
                expectedContentType,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            throw new InvalidOperationException(
                "The profile image format does not match its declared file type."
            );
        }


        // --------------------------------------------------------
        // GENERATE UNIQUE FILENAME
        // --------------------------------------------------------

        var objectName =
            $"{userId:N}_{Guid.NewGuid():N}{extension}";


        // --------------------------------------------------------
        // OPEN STREAM
        // --------------------------------------------------------

        await using var fileStream =
            file.OpenReadStream();


        // --------------------------------------------------------
        // UPLOAD TO SUPABASE
        // --------------------------------------------------------

        var publicImageUrl =
            await _supabaseStorage.UploadAsync(
                bucket:
                    SupabaseBucket,

                objectName:
                    objectName,

                source:
                    fileStream,

                contentType:
                    expectedContentType,

                maximumBytes:
                    MaxFileSize,

                cancellationToken:
                    cancellationToken
            );


        _logger.LogInformation(
            "Profile image uploaded to Supabase Storage for user {UserId}.",
            userId
        );


        return publicImageUrl;
    }


    // ============================================================
    // DELETE PROFILE IMAGE
    // ============================================================

    public async Task DeleteAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();


        if (
            string.IsNullOrWhiteSpace(
                profileImageUrl)
        )
        {
            return;
        }


        // --------------------------------------------------------
        // LEGACY LOCAL IMAGE
        // --------------------------------------------------------

        if (
            profileImageUrl.StartsWith(
                LocalProfileUrlPrefix,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            var fileName =
                profileImageUrl.Substring(
                    LocalProfileUrlPrefix.Length
                );


            if (
                string.IsNullOrWhiteSpace(
                    fileName) ||
                !string.Equals(
                    Path.GetFileName(
                        fileName),
                    fileName,
                    StringComparison.Ordinal) ||
                fileName.Contains('/') ||
                fileName.Contains('\\') ||
                fileName.Contains('?') ||
                fileName.Contains('#')
            )
            {
                return;
            }


            var physicalPath =
                Path.Combine(
                    _webRootPath,
                    "uploads",
                    "profiles",
                    fileName
                );


            if (
                File.Exists(
                    physicalPath)
            )
            {
                File.Delete(
                    physicalPath
                );


                _logger.LogInformation(
                    "Legacy local profile image deleted successfully."
                );
            }


            return;
        }


        // --------------------------------------------------------
        // SUPABASE IMAGE
        // --------------------------------------------------------

        if (
            !Uri.TryCreate(
                profileImageUrl,
                UriKind.Absolute,
                out var imageUri) ||
            imageUri.Scheme !=
                Uri.UriSchemeHttps ||
            !imageUri.AbsolutePath.StartsWith(
                SupabaseProfilePath,
                StringComparison.Ordinal)
        )
        {
            return;
        }


        await _supabaseStorage.DeleteAsync(
            profileImageUrl,
            cancellationToken
        );


        _logger.LogInformation(
            "Supabase profile image cleanup completed."
        );
    }


    // ============================================================
    // CHECK PROFILE IMAGE EXISTS
    // ============================================================

    public Task<bool> ExistsAsync(
        string? profileImageUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken
            .ThrowIfCancellationRequested();


        if (
            string.IsNullOrWhiteSpace(
                profileImageUrl)
        )
        {
            return Task.FromResult(
                false
            );
        }


        // --------------------------------------------------------
        // REMOTE / SUPABASE URL
        // --------------------------------------------------------

        if (
            !profileImageUrl.StartsWith(
                LocalProfileUrlPrefix,
                StringComparison.OrdinalIgnoreCase)
        )
        {
            return Task.FromResult(
                true
            );
        }


        // --------------------------------------------------------
        // LEGACY LOCAL IMAGE
        // --------------------------------------------------------

        var fileName =
            Path.GetFileName(
                profileImageUrl
            );


        if (
            string.IsNullOrWhiteSpace(
                fileName)
        )
        {
            return Task.FromResult(
                false
            );
        }


        var webRoot =
            _environment.WebRootPath;


        if (
            string.IsNullOrWhiteSpace(
                webRoot)
        )
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


        return Task.FromResult(
            File.Exists(
                physicalPath)
        );
    }


    // ============================================================
    // DETECT ACTUAL IMAGE FORMAT
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
            0;


        while (
            bytesRead <
            header.Length
        )
        {
            var count =
                await stream.ReadAsync(
                    header.AsMemory(
                        bytesRead,
                        header.Length -
                        bytesRead
                    ),
                    cancellationToken
                );


            if (
                count == 0
            )
            {
                break;
            }


            bytesRead +=
                count;
        }


        // --------------------------------------------------------
        // JPEG
        // --------------------------------------------------------

        if (
            bytesRead >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF
        )
        {
            return ".jpg";
        }


        // --------------------------------------------------------
        // PNG
        // --------------------------------------------------------

        if (
            bytesRead >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A
        )
        {
            return ".png";
        }


        // --------------------------------------------------------
        // WEBP
        // --------------------------------------------------------

        if (
            bytesRead >= 12 &&
            header[0] == (byte)'R' &&
            header[1] == (byte)'I' &&
            header[2] == (byte)'F' &&
            header[3] == (byte)'F' &&
            header[8] == (byte)'W' &&
            header[9] == (byte)'E' &&
            header[10] == (byte)'B' &&
            header[11] == (byte)'P'
        )
        {
            return ".webp";
        }


        return null;
    }
}