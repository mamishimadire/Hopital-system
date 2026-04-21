using MedBridge.Data;
using MedBridge.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account");

        var roles = await _users.GetRolesAsync(user);
        var role = roles.FirstOrDefault() ?? "Unknown";

        // Route to role-specific landing pages
        if (role == "Admin") return RedirectToAction("Index", "Admin");
        if (role is "Supplier") return RedirectToAction("Index", "SupplierPortal");

        // For all other roles, show the medication management dashboard
        ViewBag.UserName = user.FullName;
        ViewBag.Role = role;
        ViewBag.PendingRx = await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Pending);
        ViewBag.LowStock = await _db.Medications.CountAsync(m => m.Quantity <= m.MinimumQuantity && m.IsActive);
        ViewBag.OutOfStock = await _db.Medications.CountAsync(m => m.Quantity == 0 && m.IsActive);
        ViewBag.PendingPR = await _db.PurchaseRequisitions.CountAsync(r => r.Status == PRStatus.Pending);
        ViewBag.PendingPO = await _db.PurchaseOrders.CountAsync(p => p.Status == POStatus.Approved);
        ViewBag.UnreadFinance = await _db.FinanceInboxItems.CountAsync(f => !f.IsRead);

        var recentRx = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medication)
            .OrderByDescending(p => p.PrescribedAt)
            .Take(5)
            .ToListAsync();

        var stockAlerts = await _db.Medications
            .Where(m => m.Quantity <= m.MinimumQuantity && m.IsActive)
            .OrderBy(m => m.Quantity)
            .Take(8)
            .ToListAsync();

        ViewBag.RecentRx = recentRx;
        ViewBag.StockAlerts = stockAlerts;

        return View();
    }
}
