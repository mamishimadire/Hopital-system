using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class Patient
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}";

    [MaxLength(20)]
    public string? IdNumber { get; set; }

    public DateTime DateOfBirth { get; set; }

    [MaxLength(10)]
    public string Gender { get; set; } = "M";

    [MaxLength(20)]
    public string? Phone { get; set; }

    [MaxLength(200)]
    public string? Email { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [MaxLength(200)]
    public string? MedicalAid { get; set; }

    [MaxLength(100)]
    public string? MedicalAidNumber { get; set; }

    [MaxLength(200)]
    public string? FamilyHistory { get; set; }

    [MaxLength(200)]
    public string? Lifestyle { get; set; }

    public string HealthStatus { get; set; } = "HEALTHY";

    public int? ClientId { get; set; }
    public Client? Client { get; set; }

    public string? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<PatientCondition> Conditions { get; set; } = new List<PatientCondition>();
    public ICollection<PatientAllergy> Allergies { get; set; } = new List<PatientAllergy>();
    public ICollection<PatientVitals> VitalsHistory { get; set; } = new List<PatientVitals>();
    public ICollection<PatientScreening> Screenings { get; set; } = new List<PatientScreening>();
    public ICollection<Prescription> Prescriptions { get; set; } = new List<Prescription>();
    public ICollection<PatientSurgery> Surgeries { get; set; } = new List<PatientSurgery>();
    public ICollection<PatientNote> Notes { get; set; } = new List<PatientNote>();
}
