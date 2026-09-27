namespace Gemora.Domain.Interfaces;

public interface IGemImageReader
{
    Task<GemImageReadResult?> OpenImageAsync(
        string? imageUrl,
        CancellationToken cancellationToken = default);
}


public sealed class GemImageReadResult : IAsyncDisposable
{
    public Stream Stream { get; }

    public string ContentType { get; }


    public GemImageReadResult(
        Stream stream,
        string contentType)
    {
        Stream = stream;
        ContentType = contentType;
    }


    public async ValueTask DisposeAsync()
    {
        await Stream.DisposeAsync();
    }
}