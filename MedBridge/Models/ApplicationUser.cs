using Microsoft.AspNetCore.Identity;

namespace MedBridge.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public int? ClientId { get; set; }
    public Client? Client { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
    public string? ProfilePicture { get; set; }
}
