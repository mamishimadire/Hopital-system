using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public enum PrescriptionStatus
{
    Pending,
    Dispensed,
    Cancelled
}

public class Prescription
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string RxNumber { get; set; } = string.Empty; // RX001

    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string PrescribedByUserId { get; set; } = string.Empty;
    public ApplicationUser PrescribedBy { get; set; } = null!;

    public DateTime PrescribedAt { get; set; } = DateTime.UtcNow;

    public int MedicationId { get; set; }
    public Medication Medication { get; set; } = null!;

    public int Quantity { get; set; }

    [MaxLength(200)]
    public string? Dosage { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Pending;

    public string? DispensedByUserId { get; set; }
    public ApplicationUser? DispensedBy { get; set; }
    public DateTime? DispensedAt { get; set; }
}
