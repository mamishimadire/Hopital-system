using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public enum InvoiceStatus { PendingMatch, Matched, Rejected, Paid }

public class Invoice
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string InvoiceNumber { get; set; } = string.Empty;

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    public DateTime InvoiceDate { get; set; }
    public InvoiceStatus Status { get; set; } = InvoiceStatus.PendingMatch;

    public string UploadedByUserId { get; set; } = string.Empty;
    public ApplicationUser UploadedBy { get; set; } = null!;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public DateTime? FinanceRoutedAt { get; set; }

    public string? PaidByUserId { get; set; }
    public ApplicationUser? PaidBy { get; set; }
    public DateTime? PaidAt { get; set; }

    [MaxLength(200)]
    public string? PaymentReference { get; set; }

    [MaxLength(500)]
    public string? InvoiceFileName { get; set; }
}
