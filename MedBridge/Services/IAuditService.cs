namespace MedBridge.Services;

public interface IAuditService
{
    Task LogAsync(string action, string? entityType = null, string? entityId = null,
        string category = BlockchainCategory.System);

    Task LogMedicationAsync(string action, int medicationId, string medicationName,
        int? quantity = null, string? notes = null);

    Task LogPrescriptionAsync(string action, string rxNumber, string patientName,
        string medicationName, int quantity);
}
