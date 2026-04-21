using System.ComponentModel.DataAnnotations;
using MedBridge.Models;

namespace MedBridge.ViewModels.Admin;

public class SupplierListViewModel
{
    public int Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? RegistrationNo { get; set; }
    public string ContactEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? BankName { get; set; }
    public string PaymentTerms { get; set; } = string.Empty;
    public SupplierStatus Status { get; set; }
    public SupplierApprovalStatus ApprovalStatus { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ContactPersonName { get; set; }
}

public class CreateSupplierViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Supplier / Company Name")]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Company Registration Number")]
    public string RegistrationNo { get; set; } = string.Empty;

    [MaxLength(50)]
    [Display(Name = "Tax / VAT Number")]
    public string? TaxNumber { get; set; }

    [Required, EmailAddress]
    [Display(Name = "General Contact Email")]
    public string ContactEmail { get; set; } = string.Empty;

    [Phone]
    public string? Phone { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Display(Name = "Payment Terms")]
    public string PaymentTerms { get; set; } = "30 days";

    [Required, MaxLength(100)]
    [Display(Name = "Bank Name")]
    public string BankName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    [Display(Name = "Bank Account Number")]
    public string BankAccount { get; set; } = string.Empty;

    [MaxLength(20)]
    [Display(Name = "Branch Code")]
    public string? BankBranch { get; set; }

    // Contact person for order handling
    [Required, MaxLength(200)]
    [Display(Name = "Contact Person Name")]
    public string ContactPersonName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Contact Person Email")]
    public string ContactPersonEmail { get; set; } = string.Empty;

    [Phone]
    [Display(Name = "Contact Person Phone")]
    public string? ContactPersonPhone { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }
}
