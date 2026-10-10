namespace Gemora.Application.DTOs.Orders;

public class FulfillmentStatusHistoryDto
{
    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; } = string.Empty;

    public string ChangedByName { get; set; } = string.Empty;

    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; }
}