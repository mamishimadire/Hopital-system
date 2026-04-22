using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class BlockchainBlock
{
    public int Id { get; set; }

    public long BlockIndex { get; set; }

    [Required, MaxLength(100)]
    public string PreviousHash { get; set; } = "0";

    [Required, MaxLength(100)]
    public string Hash { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [Required]
    public string Data { get; set; } = string.Empty; // JSON audit data

    [MaxLength(500)]
    public string UserId { get; set; } = string.Empty;

    [MaxLength(100)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Action { get; set; } = string.Empty;

    public int Nonce { get; set; } = 0;

    [MaxLength(50)]
    public string Category { get; set; } = "System";

    [MaxLength(100)]
    public string? Signature { get; set; }

    public bool IsValid { get; set; } = true;
}
