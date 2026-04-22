using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public enum FinanceDocumentType { PoCopy, Grn, Invoice }

public class FinanceInboxItem
{
    public int Id { get; set; }

    public FinanceDocumentType DocumentType { get; set; }

    [MaxLength(20)]
    public string DocumentId { get; set; } = string.Empty; // PO001, GRN001, INV001

    public int PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    [MaxLength(200)]
    public string SupplierName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string MedicationName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    public string SentByUserId { get; set; } = string.Empty;
    public string SentByName { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    [MaxLength(1000)]
    public string Summary { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;
}
