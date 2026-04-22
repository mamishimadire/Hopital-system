using System.Security.Claims;
using MedBridge.Data;
using MedBridge.Models;
using Microsoft.AspNetCore.Identity;

namespace MedBridge.Services;

public class AuditService : IAuditService
{
    private readonly IBlockchainService _blockchain;
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;

    public AuditService(IBlockchainService blockchain, IHttpContextAccessor http,
        UserManager<ApplicationUser> userManager)
    {
        _blockchain = blockchain;
        _http = http;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null,
        string category = BlockchainCategory.System)
    {
        var (userId, userName, role) = GetCallerInfo();
        var data = new { action, entityType, entityId, timestamp = DateTime.UtcNow };
        await _blockchain.AddBlockAsync(action, userId, userName, role, data, category);
    }

    public async Task LogMedicationAsync(string action, int medicationId, string medicationName,
        int? quantity = null, string? notes = null)
    {
        var (userId, userName, role) = GetCallerInfo();
        var data = new
        {
            medicationId,
            medicationName,
            quantity,
            notes,
            timestamp = DateTime.UtcNow
        };
        await _blockchain.AddBlockAsync(action, userId, userName, role, data,
            BlockchainCategory.Medication);
    }

    public async Task LogPrescriptionAsync(string action, string rxNumber, string patientName,
        string medicationName, int quantity)
    {
        var (userId, userName, role) = GetCallerInfo();
        var data = new { rxNumber, patientName, medicationName, quantity, timestamp = DateTime.UtcNow };
        await _blockchain.AddBlockAsync(action, userId, userName, role, data,
            BlockchainCategory.Prescription);
    }

    private (string userId, string userName, string role) GetCallerInfo()
    {
        var ctx      = _http.HttpContext;
        var userId   = ctx?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userName = ctx?.User?.FindFirstValue(ClaimTypes.Name) ?? "System";
        var roles    = ctx?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value)
                       ?? Enumerable.Empty<string>();
        return (userId, userName, string.Join(", ", roles));
    }
}
