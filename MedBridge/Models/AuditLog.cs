using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public class AuditLog
{
    public int Id { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    [MaxLength(500)]
    public string? UserId { get; set; }

    [MaxLength(200)]
    public string UserName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Role { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Action { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? EntityType { get; set; }

    [MaxLength(100)]
    public string? EntityId { get; set; }

    // Blockchain reference for tamper-proof verification
    public long BlockchainBlockIndex { get; set; }

    [MaxLength(100)]
    public string BlockchainHash { get; set; } = string.Empty;
}
