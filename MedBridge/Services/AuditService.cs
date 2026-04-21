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

    public AuditService(IBlockchainService blockchain, IHttpContextAccessor http, UserManager<ApplicationUser> userManager)
    {
        _blockchain = blockchain;
        _http = http;
        _userManager = userManager;
    }

    public async Task LogAsync(string action, string? entityType = null, string? entityId = null)
    {
        var httpCtx = _http.HttpContext;
        var userId = httpCtx?.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "system";
        var userName = httpCtx?.User?.FindFirstValue(ClaimTypes.Name) ?? "System";
        var roles = httpCtx?.User?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
        var role = string.Join(", ", roles);

        var data = new { action, entityType, entityId, timestamp = DateTime.UtcNow };
        await _blockchain.AddBlockAsync(action, userId, userName, role, data);
    }
}
