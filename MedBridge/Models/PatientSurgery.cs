using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class PatientSurgery
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, MaxLength(300)]
    public string Procedure { get; set; } = string.Empty;

    public DateTime? Date { get; set; }

    [MaxLength(200)]
    public string? Surgeon { get; set; }

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
