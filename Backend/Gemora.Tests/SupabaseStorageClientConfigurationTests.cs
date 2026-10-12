using Gemora.Infrastructure.Services;

namespace Gemora.Tests;

public sealed class SupabaseStorageClientConfigurationTests
{
    [Fact]
    public void Constructor_AllowsMissingStorageConfiguration()
    {
        using var httpClient = new HttpClient();

        var exception = Record.Exception(
            () => new SupabaseStorageClient(
                httpClient,
                new SupabaseStorageOptions()
            )
        );

        Assert.Null(exception);
    }

    [Fact]
    public async Task UploadAsync_RejectsMissingConfigurationWhenStorageIsUsed()
    {
        using var httpClient = new HttpClient();

        var client = new SupabaseStorageClient(
            httpClient,
            new SupabaseStorageOptions()
        );

        await using var source = new MemoryStream(new byte[] { 1 });

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => client.UploadAsync(
                "profile-images",
                "test.png",
                source,
                "image/png",
                1024
            )
        );

        Assert.Equal(
            "Supabase Storage project URL is missing.",
            exception.Message
        );
    }
}
