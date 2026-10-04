using Gemora.Domain.Interfaces;

namespace Gemora.Infrastructure.Services;

public class LocalGemImageReader : IGemImageReader
{
    private readonly string _gemImageDirectory;


    public LocalGemImageReader(
        string uploadRoot)
    {
        if (string.IsNullOrWhiteSpace(uploadRoot))
        {
            throw new ArgumentException(
                "Upload root is required.",
                nameof(uploadRoot));
        }


        _gemImageDirectory =
            Path.GetFullPath(
                Path.Combine(
                    uploadRoot,
                    "gem-images"));
    }


    public Task<GemImageReadResult?> OpenImageAsync(
        string? imageUrl,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();


        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        const string allowedPrefix =
            "/uploads/gem-images/";


        if (!imageUrl.StartsWith(
                allowedPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        var fileName =
            Path.GetFileName(imageUrl);


        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        var extension =
            Path.GetExtension(fileName)
                .ToLowerInvariant();


        var contentType =
            extension switch
            {
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => null
            };


        if (contentType == null)
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        var filePath =
            Path.GetFullPath(
                Path.Combine(
                    _gemImageDirectory,
                    fileName));


        var allowedDirectoryPrefix =
            _gemImageDirectory.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;


        // Prevent reading files outside the trusted
        // gem-images directory.
        if (!filePath.StartsWith(
                allowedDirectoryPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        if (!File.Exists(filePath))
        {
            return Task.FromResult<
                GemImageReadResult?>(null);
        }


        Stream stream =
            new FileStream(
                filePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 81920,
                useAsync: true);


        var result =
            new GemImageReadResult(
                stream,
                contentType);


        return Task.FromResult<
            GemImageReadResult?>(result);
    }
}