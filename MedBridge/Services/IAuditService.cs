namespace MedBridge.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null);
}
