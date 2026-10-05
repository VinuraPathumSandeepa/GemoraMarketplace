using System.ComponentModel.DataAnnotations;

namespace Gemora.Domain.Entities;

public class ShipmentTrackingEvent
{
    [Key]
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    
    // Event Details
    public string EventType { get; set; } = string.Empty; // Created, PickedUp, InTransit, OutForDelivery, Delivered, Exception, Returned, PlanApproved, PlanRejected, RevisionRequested, Booked, InsuranceCreated
    
    [StringLength(500)]
    public string Location { get; set; } = string.Empty;
    
    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    [StringLength(100)]
    public string? ExternalEventCode { get; set; }

    // Audit Fields
    public Guid? PerformedByUserId { get; set; }
    
    [StringLength(50)]
    public string? PerformedByRole { get; set; }
    
    [StringLength(100)]
    public string? PreviousState { get; set; }
    
    [StringLength(100)]
    public string? NewState { get; set; }
    
    [StringLength(2000)]
    public string? Reason { get; set; }

    [Required]
    public DateTime OccurredAt { get; set; }

    [Required]
    public DateTime RecordedAt { get; set; }

    // Navigation property
    public virtual Shipment Shipment { get; set; } = null!;
}
