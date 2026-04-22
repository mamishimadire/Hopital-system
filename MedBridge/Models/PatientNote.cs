using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class PatientNote
{
    public int Id { get; set; }
    public int PatientId { get; set; }
    public Patient Patient { get; set; } = null!;

    [Required]
    public string Content { get; set; } = string.Empty;

    public string? CreatedByUserId { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
