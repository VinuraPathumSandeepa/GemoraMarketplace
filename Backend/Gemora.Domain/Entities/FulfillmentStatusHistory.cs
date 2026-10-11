namespace Gemora.Domain.Entities;

public class FulfillmentStatusHistory
{
    public int Id { get; set; }

    public Guid OrderId { get; set; }

    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public Guid? ChangedByUserId { get; set; }

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }

    public Order Order { get; set; } = null!;

    public User? ChangedByUser { get; set; }
}