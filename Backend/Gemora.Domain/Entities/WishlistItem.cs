namespace Gemora.Domain.Entities;

public class WishlistItem
{
    public Guid UserId { get; set; }
    public int GemListingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
