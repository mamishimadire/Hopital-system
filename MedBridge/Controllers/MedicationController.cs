using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedBridge.Data;
using MedBridge.Models;
using MedBridge.Services;

namespace MedBridge.Controllers;

[Authorize]
public class MedicationController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public MedicationController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ── List all medications
    [Authorize(Roles = "Admin,Pharmacist,Doctor,Nurse,ProcurementOfficer,HeadOfProcurement,HeadOfSupplyChain,Auditor")]
    public async Task<IActionResult> Index(string? search, string? filter)
    {
        var query = _db.Medications.Include(m => m.Supplier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(m => m.Name.Contains(search) || m.Code.Contains(search));

        query = filter switch
        {
            "low"     => query.Where(m => m.Quantity > 0 && m.Quantity <= m.MinimumQuantity),
            "out"     => query.Where(m => m.Quantity == 0),
            "expiring"=> query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value < DateTime.Today.AddMonths(3)),
            "expired" => query.Where(m => m.ExpiryDate.HasValue && m.ExpiryDate.Value < DateTime.Today),
            _         => query.Where(m => m.IsActive)
        };

        var meds = await query.OrderBy(m => m.Name).ToListAsync();

        ViewBag.Search = search;
        ViewBag.Filter = filter;
        ViewBag.TotalActive    = await _db.Medications.CountAsync(m => m.IsActive);
        ViewBag.TotalLowStock  = await _db.Medications.CountAsync(m => m.IsActive && m.Quantity > 0 && m.Quantity <= m.MinimumQuantity);
        ViewBag.TotalOutOfStock= await _db.Medications.CountAsync(m => m.IsActive && m.Quantity == 0);
        ViewBag.TotalExpiring  = await _db.Medications.CountAsync(m => m.IsActive && m.ExpiryDate.HasValue && m.ExpiryDate.Value < DateTime.Today.AddMonths(3));

        return View(meds);
    }

    // ── Create
    [Authorize(Roles = "Admin,Pharmacist,ProcurementOfficer")]
    public async Task<IActionResult> Create()
    {
        ViewBag.Suppliers = new SelectList(
            await _db.Suppliers.Where(s => s.Status == SupplierStatus.Active).ToListAsync(),
            "Id", "Name");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Pharmacist,ProcurementOfficer")]
    public async Task<IActionResult> Create(Medication model)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = new SelectList(
                await _db.Suppliers.Where(s => s.Status == SupplierStatus.Active).ToListAsync(),
                "Id", "Name");
            return View(model);
        }

        if (await _db.Medications.AnyAsync(m => m.Code == model.Code))
        {
            ModelState.AddModelError("Code", "Medication code already exists.");
            ViewBag.Suppliers = new SelectList(
                await _db.Suppliers.Where(s => s.Status == SupplierStatus.Active).ToListAsync(),
                "Id", "Name");
            return View(model);
        }

        model.CreatedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        model.CreatedAt = DateTime.UtcNow;
        _db.Medications.Add(model);
        await _db.SaveChangesAsync();

        await _audit.LogMedicationAsync(
            $"Medication created: {model.Name} ({model.Code}) — initial stock {model.Quantity} {model.Unit}",
            model.Id, model.Name, model.Quantity, $"UnitCost: R{model.UnitCost}");

        TempData["Success"] = $"Medication '{model.Name}' added successfully.";
        return RedirectToAction(nameof(Index));
    }

    // ── Edit
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> Edit(int id)
    {
        var med = await _db.Medications.FindAsync(id);
        if (med == null) return NotFound();

        ViewBag.Suppliers = new SelectList(
            await _db.Suppliers.Where(s => s.Status == SupplierStatus.Active).ToListAsync(),
            "Id", "Name", med.SupplierId);
        return View(med);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Pharmacist")]
    public async Task<IActionResult> Edit(int id, Medication model)
    {
        if (id != model.Id) return BadRequest();

        if (!ModelState.IsValid)
        {
            ViewBag.Suppliers = new SelectList(
                await _db.Suppliers.Where(s => s.Status == SupplierStatus.Active).ToListAsync(),
                "Id", "Name");
            return View(model);
        }

        var existing = await _db.Medications.FindAsync(id);
        if (existing == null) return NotFound();

        existing.Name           = model.Name;
        existing.BatchNumber    = model.BatchNumber;
        existing.ExpiryDate     = model.ExpiryDate;
        existing.MinimumQuantity= model.MinimumQuantity;
        existing.Unit           = model.Unit;
        existing.UnitCost       = model.UnitCost;
        existing.SupplierId     = model.SupplierId;
        existing.IsActive       = model.IsActive;

        await _db.SaveChangesAsync();

        await _audit.LogMedicationAsync(
            $"Medication updated: {existing.Name} ({existing.Code})",
            existing.Id, existing.Name);

        TempData["Success"] = $"Medication '{existing.Name}' updated.";
        return RedirectToAction(nameof(Index));
    }

    // ── Dispense (reduce stock directly — for walk-in or ward dispensing)
    [Authorize(Roles = "Pharmacist,Nurse")]
    public async Task<IActionResult> Dispense(int id)
    {
        var med = await _db.Medications.Include(m => m.Supplier).FirstOrDefaultAsync(m => m.Id == id);
        if (med == null) return NotFound();
        return View(med);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Pharmacist,Nurse")]
    public async Task<IActionResult> Dispense(int id, int quantity, string? notes)
    {
        var med = await _db.Medications.FindAsync(id);
        if (med == null) return NotFound();

        if (quantity <= 0)
        {
            TempData["Error"] = "Quantity must be greater than zero.";
            return RedirectToAction(nameof(Dispense), new { id });
        }

        if (quantity > med.Quantity)
        {
            TempData["Error"] = $"Insufficient stock. Available: {med.Quantity} {med.Unit}.";
            return RedirectToAction(nameof(Dispense), new { id });
        }

        var stockBefore = med.Quantity;
        med.Quantity -= quantity;
        await _db.SaveChangesAsync();

        await _audit.LogMedicationAsync(
            $"Medication dispensed: {med.Name} — {quantity} {med.Unit} (stock: {stockBefore} → {med.Quantity})",
            med.Id, med.Name, quantity, notes);

        TempData["Success"] = $"Dispensed {quantity} {med.Unit} of {med.Name}.";
        return RedirectToAction(nameof(Index));
    }

    // ── Adjust Stock (restock / write-off)
    [Authorize(Roles = "Admin,Pharmacist,ReceivingClerk")]
    public async Task<IActionResult> AdjustStock(int id)
    {
        var med = await _db.Medications.FindAsync(id);
        if (med == null) return NotFound();
        return View(med);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Pharmacist,ReceivingClerk")]
    public async Task<IActionResult> AdjustStock(int id, int adjustment, string reason)
    {
        var med = await _db.Medications.FindAsync(id);
        if (med == null) return NotFound();

        var stockBefore = med.Quantity;
        med.Quantity = Math.Max(0, med.Quantity + adjustment);
        await _db.SaveChangesAsync();

        var direction = adjustment >= 0 ? "Stock added" : "Stock removed";
        await _audit.LogMedicationAsync(
            $"{direction}: {med.Name} — {Math.Abs(adjustment)} {med.Unit} (stock: {stockBefore} → {med.Quantity}) | Reason: {reason}",
            med.Id, med.Name, adjustment, reason);

        TempData["Success"] = $"Stock adjusted. {med.Name} now has {med.Quantity} {med.Unit}.";
        return RedirectToAction(nameof(Index));
    }
}
