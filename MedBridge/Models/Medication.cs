using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public class Medication
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string Code { get; set; } = string.Empty; // MED001

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? BatchNumber { get; set; }

    public DateTime? ExpiryDate { get; set; }

    public int Quantity { get; set; } = 0;

    public int MinimumQuantity { get; set; } = 20;

    [MaxLength(50)]
    public string Unit { get; set; } = "tablets";

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitCost { get; set; }

    public bool IsActive { get; set; } = true;
    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Computed helpers (not mapped)
    [NotMapped]
    public bool IsOutOfStock => Quantity == 0;

    [NotMapped]
    public bool IsLowStock => Quantity > 0 && Quantity <= MinimumQuantity;

    [NotMapped]
    public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today;

    [NotMapped]
    public bool IsExpiringSoon => ExpiryDate.HasValue && ExpiryDate.Value < DateTime.Today.AddMonths(3) && !IsExpired;
}
