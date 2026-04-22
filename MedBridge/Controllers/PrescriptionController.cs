using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MedBridge.Data;
using MedBridge.Models;
using MedBridge.Services;

namespace MedBridge.Controllers;

[Authorize]
public class PrescriptionController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAuditService _audit;

    public PrescriptionController(ApplicationDbContext db, IAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    // ── List prescriptions (role-filtered)
    [Authorize(Roles = "Admin,Doctor,Nurse,Pharmacist")]
    public async Task<IActionResult> Index(string? status, string? search)
    {
        var query = _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medication)
            .Include(p => p.PrescribedBy)
            .Include(p => p.DispensedBy)
            .AsQueryable();

        if (Enum.TryParse<PrescriptionStatus>(status, out var s))
            query = query.Where(p => p.Status == s);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(p =>
                p.RxNumber.Contains(search) ||
                p.Patient.FirstName.Contains(search) ||
                p.Patient.LastName.Contains(search) ||
                p.Medication.Name.Contains(search));

        var prescriptions = await query.OrderByDescending(p => p.PrescribedAt).ToListAsync();

        ViewBag.StatusFilter  = status;
        ViewBag.Search        = search;
        ViewBag.PendingCount  = await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Pending);
        ViewBag.DispensedCount= await _db.Prescriptions.CountAsync(p => p.Status == PrescriptionStatus.Dispensed);

        return View(prescriptions);
    }

    // ── Create prescription (Doctor / Admin)
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdowns();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Create(Prescription model)
    {
        model.PrescribedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value!;
        model.PrescribedAt = DateTime.UtcNow;
        model.Status = PrescriptionStatus.Pending;

        // Generate RxNumber
        var count = await _db.Prescriptions.CountAsync();
        model.RxNumber = $"RX{(count + 1):D5}";

        // Validate patient and medication exist
        var patient  = await _db.Patients.FindAsync(model.PatientId);
        var med      = await _db.Medications.FindAsync(model.MedicationId);

        if (patient == null) ModelState.AddModelError("PatientId", "Patient not found.");
        if (med == null)     ModelState.AddModelError("MedicationId", "Medication not found.");

        if (!ModelState.IsValid)
        {
            await PopulateDropdowns();
            return View(model);
        }

        _db.Prescriptions.Add(model);
        await _db.SaveChangesAsync();

        await _audit.LogPrescriptionAsync(
            $"Prescription created: {model.RxNumber} — {med!.Name} × {model.Quantity} for {patient!.FullName}",
            model.RxNumber, patient.FullName, med.Name, model.Quantity);

        TempData["Success"] = $"Prescription {model.RxNumber} created for {patient.FullName}.";
        return RedirectToAction(nameof(Index));
    }

    // ── Dispense prescription (Pharmacist)
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Pharmacist,Admin")]
    public async Task<IActionResult> Dispense(int id)
    {
        var rx = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medication)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (rx == null) return NotFound();

        if (rx.Status != PrescriptionStatus.Pending)
        {
            TempData["Error"] = "This prescription has already been dispensed or cancelled.";
            return RedirectToAction(nameof(Index));
        }

        var med = rx.Medication;
        if (med.Quantity < rx.Quantity)
        {
            TempData["Error"] = $"Insufficient stock for {med.Name}. Available: {med.Quantity} {med.Unit}, required: {rx.Quantity}.";
            return RedirectToAction(nameof(Index));
        }

        var stockBefore = med.Quantity;
        med.Quantity -= rx.Quantity;

        rx.Status            = PrescriptionStatus.Dispensed;
        rx.DispensedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        rx.DispensedAt       = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        await _audit.LogPrescriptionAsync(
            $"Prescription dispensed: {rx.RxNumber} — {med.Name} × {rx.Quantity} for {rx.Patient.FullName} (stock: {stockBefore} → {med.Quantity})",
            rx.RxNumber, rx.Patient.FullName, med.Name, rx.Quantity);

        TempData["Success"] = $"Prescription {rx.RxNumber} dispensed. {med.Name} stock updated to {med.Quantity} {med.Unit}.";
        return RedirectToAction(nameof(Index));
    }

    // ── Cancel prescription
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin,Doctor")]
    public async Task<IActionResult> Cancel(int id)
    {
        var rx = await _db.Prescriptions
            .Include(p => p.Patient)
            .Include(p => p.Medication)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (rx == null) return NotFound();

        if (rx.Status == PrescriptionStatus.Dispensed)
        {
            TempData["Error"] = "Cannot cancel a prescription that has already been dispensed.";
            return RedirectToAction(nameof(Index));
        }

        rx.Status = PrescriptionStatus.Cancelled;
        await _db.SaveChangesAsync();

        await _audit.LogPrescriptionAsync(
            $"Prescription cancelled: {rx.RxNumber} — {rx.Medication.Name} for {rx.Patient.FullName}",
            rx.RxNumber, rx.Patient.FullName, rx.Medication.Name, rx.Quantity);

        TempData["Success"] = $"Prescription {rx.RxNumber} cancelled.";
        return RedirectToAction(nameof(Index));
    }

    private async Task PopulateDropdowns()
    {
        ViewBag.Patients = new SelectList(
            await _db.Patients.OrderBy(p => p.LastName).ToListAsync(),
            "Id", "FullName");
        ViewBag.Medications = new SelectList(
            await _db.Medications.Where(m => m.IsActive && m.Quantity > 0).OrderBy(m => m.Name).ToListAsync(),
            "Id", "Name");
    }
}
