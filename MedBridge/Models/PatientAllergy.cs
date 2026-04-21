using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class PatientAllergy
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required, MaxLength(200)]
    public string Allergen { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Severity { get; set; } = "Mild"; // Mild, Moderate, Severe

    [MaxLength(200)]
    public string? Reaction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
