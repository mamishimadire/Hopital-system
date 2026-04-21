using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class PatientScreening
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, MaxLength(200)]
    public string TestName { get; set; } = string.Empty;

    public DateTime? LastPerformed { get; set; }
    public DateTime? NextDue { get; set; }

    [MaxLength(200)]
    public string? Result { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
