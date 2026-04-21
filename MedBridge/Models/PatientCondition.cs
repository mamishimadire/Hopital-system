using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class PatientCondition
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? IcdCode { get; set; }

    [MaxLength(10)]
    public string? Since { get; set; }

    [MaxLength(200)]
    public string? CurrentMedication { get; set; }

    public string Status { get; set; } = "active"; // active, resolved

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
