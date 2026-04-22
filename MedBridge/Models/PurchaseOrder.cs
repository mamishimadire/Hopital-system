using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public enum POStatus { Pending, Approved, Sent, Received, Cancelled }
public enum POFinanceStatus { NotNotified, PoReceived, GrnReceived, InvoiceReceived, ReadyForMatch, Matched, Paid }

public class PurchaseOrder
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string PONumber { get; set; } = string.Empty; // PO001

    public int? PurchaseRequisitionId { get; set; }
    public PurchaseRequisition? PurchaseRequisition { get; set; }

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public int Quantity { get; set; }

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    public POStatus Status { get; set; } = POStatus.Pending;

    public string CreatedByUserId { get; set; } = string.Empty;
    public ApplicationUser CreatedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? SentByUserId { get; set; }
    public ApplicationUser? SentBy { get; set; }
    public DateTime? SentAt { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    // Finance routing
    public DateTime? FinanceNotifiedAt { get; set; }
    public POFinanceStatus FinanceStatus { get; set; } = POFinanceStatus.NotNotified;

    public int ReceivedQuantity { get; set; } = 0;

    // Supplier acknowledgement
    public bool SupplierAcknowledged { get; set; } = false;
    public DateTime? SupplierAcknowledgedAt { get; set; }
    public DateTime? EstimatedDelivery { get; set; }

    [NotMapped]
    public decimal TotalAmount => UnitCost * Quantity;

    // Navigation
    public ICollection<GoodsReceiptNote> GoodsReceiptNotes { get; set; } = new List<GoodsReceiptNote>();
    public Invoice? Invoice { get; set; }
}
