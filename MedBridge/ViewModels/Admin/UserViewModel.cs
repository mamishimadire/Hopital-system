using System.ComponentModel.DataAnnotations;

namespace MedBridge.ViewModels.Admin;

public class UserListViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? ClientName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLogin { get; set; }
    public string? JobTitle { get; set; }
}

public class CreateUserViewModel
{
    [Required, MaxLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    [Display(Name = "Email / Username")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Client / Organisation")]
    public int? ClientId { get; set; }

    [MaxLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }
}

public class EditUserViewModel
{
    public string Id { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = string.Empty;

    [Display(Name = "Client / Organisation")]
    public int? ClientId { get; set; }

    [MaxLength(100)]
    [Display(Name = "Job Title")]
    public string? JobTitle { get; set; }

    [MaxLength(100)]
    public string? Department { get; set; }

    [Phone]
    [Display(Name = "Phone Number")]
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
