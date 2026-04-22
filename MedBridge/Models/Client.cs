using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class Client
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(50)]
    public string Type { get; set; } = "Clinic"; // Hospital, Clinic, Practice

    [MaxLength(100)]
    public string? RegistrationNo { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(200)]
    public string? Website { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedByUserId { get; set; }

    // Navigation
    public ICollection<ApplicationUser> Staff { get; set; } = new List<ApplicationUser>();
    public ICollection<Patient> Patients { get; set; } = new List<Patient>();
}
