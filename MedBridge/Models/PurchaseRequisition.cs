using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public enum PRStatus { Pending, Approved, Rejected, Converted }
public enum PRUrgency { Normal, High, Critical }

public class PurchaseRequisition
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string PRNumber { get; set; } = string.Empty; // PR001

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public int QuantityRequested { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    [Required, MaxLength(1000)]
    public string Justification { get; set; } = string.Empty;

    public PRUrgency Urgency { get; set; } = PRUrgency.Normal;
    public PRStatus Status { get; set; } = PRStatus.Pending;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? RejectedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    // Reverse navigation — EF manages this via PurchaseOrder.PurchaseRequisitionId
    public PurchaseOrder? PurchaseOrder { get; set; }
}
