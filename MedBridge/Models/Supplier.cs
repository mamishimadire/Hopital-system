using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public enum SupplierStatus { Active, Suspended }
public enum SupplierApprovalStatus { Pending, Approved, Rejected }

public class Supplier
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string SupplierCode { get; set; } = string.Empty; // SUP001

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? RegistrationNo { get; set; }

    [MaxLength(50)]
    public string? TaxNumber { get; set; }

    [MaxLength(200)]
    public string ContactEmail { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(100)]
    public string? AccountNumber { get; set; }

    [MaxLength(50)]
    public string PaymentTerms { get; set; } = "30 days";

    [MaxLength(100)]
    public string? BankName { get; set; }

    [MaxLength(100)]
    public string? BankAccount { get; set; }

    [MaxLength(20)]
    public string? BankBranch { get; set; }

    public SupplierStatus Status { get; set; } = SupplierStatus.Active;
    public SupplierApprovalStatus ApprovalStatus { get; set; } = SupplierApprovalStatus.Pending;

    public string? ApprovedByUserId { get; set; }
    public ApplicationUser? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? SubmittedByUserId { get; set; }
    public ApplicationUser? SubmittedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Contact person for orders
    [MaxLength(200)]
    public string? ContactPersonName { get; set; }

    [MaxLength(200)]
    public string? ContactPersonEmail { get; set; }

    [MaxLength(20)]
    public string? ContactPersonPhone { get; set; }

    // Portal user account linked to this supplier
    public string? PortalUserId { get; set; }

    public string? Notes { get; set; }

    // Navigation
    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
    public ICollection<SupplierDocument> Documents { get; set; } = new List<SupplierDocument>();
}

public class SupplierDocument
{
    public int Id { get; set; }
    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    [MaxLength(200)]
    public string FileName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string DocumentType { get; set; } = string.Empty; // Registration, Bank, Tax

    public string? UploadedByUserId { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
