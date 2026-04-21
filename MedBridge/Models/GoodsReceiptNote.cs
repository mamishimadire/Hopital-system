using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public enum GRNCondition { Good, Partial, Rejected }

public class GoodsReceiptNote
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string GRNNumber { get; set; } = string.Empty; // GRN001

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public int QuantityOrdered { get; set; }
    public int QuantityReceived { get; set; }

    [MaxLength(100)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public GRNCondition Condition { get; set; } = GRNCondition.Good;

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public string ReceivedByUserId { get; set; } = string.Empty;
    public ApplicationUser ReceivedBy { get; set; } = null!;
    public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;

    public int StockBefore { get; set; }
    public int StockAfter { get; set; }
    public bool Discrepancy { get; set; } = false;
    public bool OverReceived { get; set; } = false;

    public DateTime? FinanceNotifiedAt { get; set; }
}
