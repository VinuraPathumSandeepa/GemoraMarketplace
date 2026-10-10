using Gemora.Domain.Entities;
using Gemora.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Gemora.API.Services;

// Retains existing URLs while storing all new photo bytes in PostgreSQL.
public sealed class DatabaseProfileImageStorageService(
    ApplicationDbContext db,
    ProfileImageStorageService legacy) : IProfileImageStorageService
{
    private const int MaxBytes = 5 * 1024 * 1024;
    private const string Prefix = "/api/profile-images/";

    public async Task<string> SaveAsync(Guid userId, IFormFile file, CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
            throw new InvalidOperationException("Please select a profile image.");
        if (file.Length > MaxBytes)
            throw new InvalidOperationException("The profile image must be 5 MB or smaller.");
        await using var source = file.OpenReadStream();
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await source.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxBytes)
                throw new InvalidOperationException("The profile image must be 5 MB or smaller.");
            buffer.Write(chunk, 0, read);
        }
        var bytes = buffer.ToArray();
        var type = DetectContentType(bytes);
        if (type == null || !string.Equals(type, file.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Please select a valid JPG, PNG, or WebP image matching its file type.");
        var image = new ProfileImage { UserId = userId, Content = bytes, ContentType = type };
        db.ProfileImages.Add(image);
        await db.SaveChangesAsync(cancellationToken);
        return Prefix + image.Id;
    }

    public async Task DeleteAsync(string? url, CancellationToken cancellationToken = default)
    {
        if (!TryId(url, out var id)) { await legacy.DeleteAsync(url, cancellationToken); return; }
        var image = await db.ProfileImages.FindAsync(new object[] { id }, cancellationToken);
        if (image == null) return;
        db.ProfileImages.Remove(image);
        await db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(string? url, CancellationToken cancellationToken = default)
        => TryId(url, out var id)
            ? db.ProfileImages.AnyAsync(x => x.Id == id, cancellationToken)
            : legacy.ExistsAsync(url, cancellationToken);

    private static bool TryId(string? url, out Guid id)
    {
        id = default;
        return url?.StartsWith(Prefix, StringComparison.Ordinal) == true && Guid.TryParse(url[Prefix.Length..], out id);
    }

    private static string? DetectContentType(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff) return "image/jpeg";
        if (bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) return "image/png";
        if (bytes.Length >= 12 && bytes.AsSpan(0, 4).SequenceEqual("RIFF"u8) && bytes.AsSpan(8, 4).SequenceEqual("WEBP"u8)) return "image/webp";
        return null;
    }
}
