using System.ComponentModel.DataAnnotations;

namespace MedBridge.ViewModels.Admin;

public class ClientListViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? RegistrationNo { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public int StaffCount { get; set; }
    public int PatientCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateClientViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Organisation Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Type")]
    public string Type { get; set; } = "Clinic"; // Hospital, Clinic, Practice

    [MaxLength(100)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNo { get; set; }

    [MaxLength(500)]
    public string? Address { get; set; }

    [Phone]
    public string? Phone { get; set; }

    [EmailAddress]
    public string? Email { get; set; }

    [Url]
    public string? Website { get; set; }
}
