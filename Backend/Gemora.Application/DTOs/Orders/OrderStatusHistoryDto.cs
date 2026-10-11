namespace Gemora.Application.DTOs.Orders;

public class OrderStatusHistoryDto
{
    public string? PreviousStatus { get; set; }

    public string NewStatus { get; set; }
        = string.Empty;

    public string ChangedByName { get; set; }
        = string.Empty;

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; }
}