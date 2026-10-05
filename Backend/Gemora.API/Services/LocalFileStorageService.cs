using Gemora.Application.Interfaces;

namespace Gemora.API.Services;

public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly string _storageRoot;

    public LocalFileStorageService(IWebHostEnvironment environment)
    {
        _storageRoot = Path.GetFullPath(Path.Combine(
            environment.ContentRootPath,
            "App_Data",
            "compliance-documents"
        ));

        Directory.CreateDirectory(_storageRoot);
    }

    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken = default)
    {
        if (content == null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (string.IsNullOrWhiteSpace(extension))
        {
            throw new ArgumentException("Extension cannot be null or empty.", nameof(extension));
        }

        var normalizedExt = extension.Trim().ToLowerInvariant();
        if (!normalizedExt.StartsWith("."))
        {
            normalizedExt = "." + normalizedExt;
        }

        if (normalizedExt.Contains("/") || normalizedExt.Contains("\\"))
        {
            throw new ArgumentException("Extension contains invalid path characters.", nameof(extension));
        }

        var storageKey = $"{Guid.NewGuid():N}{normalizedExt}";
        var fullPath = GetSafeFullPath(storageKey);

        using (var destinationStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            useAsync: true))
        {
            await content.CopyToAsync(destinationStream, cancellationToken);
        }

        return storageKey;
    }

    public Task<Stream?> OpenReadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.FromResult<Stream?>(null);
        }

        try
        {
            var fullPath = GetSafeFullPath(storageKey);

            if (!File.Exists(fullPath))
            {
                return Task.FromResult<Stream?>(null);
            }

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                bufferSize: 4096,
                useAsync: true);

            return Task.FromResult<Stream?>(stream);
        }
        catch (ArgumentException)
        {
            return Task.FromResult<Stream?>(null);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult<Stream?>(null);
        }
    }

    public Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return Task.CompletedTask;
        }

        try
        {
            var fullPath = GetSafeFullPath(storageKey);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }
        }
        catch (ArgumentException)
        {
            // Ignore invalid storage keys on delete
        }
        catch (InvalidOperationException)
        {
            // Ignore invalid storage keys on delete
        }

        return Task.CompletedTask;
    }

    private string GetSafeFullPath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key cannot be null or empty.", nameof(storageKey));
        }

        var fileName = Path.GetFileName(storageKey);
        if (fileName != storageKey || storageKey.Contains("/") || storageKey.Contains("\\"))
        {
            throw new ArgumentException("Storage key contains invalid path characters.", nameof(storageKey));
        }

        var fullPath = Path.GetFullPath(Path.Combine(_storageRoot, fileName));

        var rootWithSeparator = _storageRoot.EndsWith(Path.DirectorySeparatorChar.ToString())
            ? _storageRoot
            : _storageRoot + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Access outside storage directory is forbidden.");
        }

        return fullPath;
    }
}

