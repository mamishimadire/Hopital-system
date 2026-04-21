using System.ComponentModel.DataAnnotations.Schema;

namespace MedBridge.Models;

public class PatientVitals
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    public string? BloodPressure { get; set; }
    public int? Pulse { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Temperature { get; set; }
    [Column(TypeName = "decimal(6,2)")]
    public decimal? Weight { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Height { get; set; }
    [Column(TypeName = "decimal(5,2)")]
    public decimal? Bmi { get; set; }
    public string? Notes { get; set; }

    public string? RecordedByUserId { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
