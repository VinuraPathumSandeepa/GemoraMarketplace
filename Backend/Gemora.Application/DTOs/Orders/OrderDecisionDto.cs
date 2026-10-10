using System.ComponentModel.DataAnnotations;

namespace Gemora.Application.DTOs.Orders;

public class OrderDecisionDto
{
    [MaxLength(1000)]
    public string? Reason { get; set; }
    public bool AlreadySold { get; set; }
}

public class ReadOrderMessageDto
{
    public DateTime MessageAt { get; set; }
}
