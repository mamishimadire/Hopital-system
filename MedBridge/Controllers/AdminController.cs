using MedBridge.Data;
using MedBridge.Models;
using MedBridge.Services;
using MedBridge.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole> _roles;
    private readonly IAuditService _audit;
    private readonly IBlockchainService _blockchain;
    private readonly IEmailService _email;
    private readonly ITokenUrlService _tokenUrl;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        RoleManager<IdentityRole> roles, IAuditService audit, IBlockchainService blockchain,
        IEmailService email, ITokenUrlService tokenUrl)
    {
        _db         = db;
        _users      = users;
        _roles      = roles;
        _audit      = audit;
        _blockchain = blockchain;
        _email    = email;
        _tokenUrl = tokenUrl;
    }

    // ── Admin Home Dashboard
    public async Task<IActionResult> Index()
    {
        var adminUser = await _users.GetUserAsync(User);
        ViewBag.AdminName = adminUser?.FullName ?? "Admin";
        ViewBag.TotalUsers = await _users.Users.CountAsync();
        ViewBag.TotalClients = await _db.Clients.CountAsync();
        ViewBag.TotalSuppliers = await _db.Suppliers.CountAsync(s => s.ApprovalStatus == SupplierApprovalStatus.Approved);
        ViewBag.PendingSuppliers = await _db.Suppliers.CountAsync(s => s.ApprovalStatus == SupplierApprovalStatus.Pending);
        ViewBag.TotalMedications = await _db.Medications.CountAsync(m => m.IsActive);
        ViewBag.AuditCount = await _db.AuditLogs.CountAsync();

        var recentUsers = await _users.Users
            .OrderByDescending(u => u.CreatedAt).Take(6).ToListAsync();
        var recentUserVms = new List<object>();
        foreach (var u in recentUsers)
        {
            var roles = await _users.GetRolesAsync(u);
            recentUserVms.Add(new { u.FullName, u.Email, Role = roles.FirstOrDefault() ?? "None", u.IsActive });
        }
        ViewBag.RecentUsers = recentUserVms;

        ViewBag.PendingSupplierList = await _db.Suppliers
            .Where(s => s.ApprovalStatus == SupplierApprovalStatus.Pending)
            .Select(s => new { s.Id, s.Name, s.ContactEmail, s.RegistrationNo })
            .Take(10).ToListAsync();

        return View();
    }

    // ══════════════════════════════════════════════
    // USERS
    // ══════════════════════════════════════════════
    public async Task<IActionResult> Users(string? search, string? role, bool? active)
    {
        var query = _users.Users.Include(u => u.Client).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
        if (!string.IsNullOrEmpty(role))
        {
            var usersInRole = await _users.GetUsersInRoleAsync(role);
            var ids = usersInRole.Select(u => u.Id).ToHashSet();
            query = query.Where(u => ids.Contains(u.Id));
        }
        if (active.HasValue)
            query = query.Where(u => u.IsActive == active.Value);

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var list = new List<UserListViewModel>();
        foreach (var u in users)
        {
            var roles = await _users.GetRolesAsync(u);
            list.Add(new UserListViewModel
            {
                Id = u.Id, FullName = u.FullName, Email = u.Email ?? "",
                Role = roles.FirstOrDefault() ?? "None",
                ClientName = u.Client?.Name, IsActive = u.IsActive,
                CreatedAt = u.CreatedAt, LastLogin = u.LastLogin,
                JobTitle = u.JobTitle
            });
        }

        ViewBag.Roles = DbInitializer.Roles;
        ViewBag.Search = search;
        ViewBag.SelectedRole = role;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> CreateUser()
    {
        await PopulateUserDropdowns();
        return View(new CreateUserViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateUserDropdowns(); return View(model); }

        var user = new ApplicationUser
        {
            UserName = model.Email, Email = model.Email, FullName = model.FullName,
            EmailConfirmed = true, IsActive = true, ClientId = model.ClientId,
            JobTitle = model.JobTitle, Department = model.Department,
            PhoneNumber = model.PhoneNumber, CreatedAt = DateTime.UtcNow,
            MustChangePassword = true  // force password change on first login
        };

        var result = await _users.CreateAsync(user, model.Password);
        if (result.Succeeded)
        {
            await _users.AddToRoleAsync(user, model.Role);
            await _audit.LogAsync($"Admin created user {model.Email} with role {model.Role}", "User", user.Id);

            // Send welcome email with temporary password
            try { await _email.SendWelcomeAsync(model.Email, model.FullName, model.Password); }
            catch { /* email failure must not block user creation */ }

            TempData["Success"] = $"User {model.FullName} created. A welcome email with login details has been sent to {model.Email}.";
            return RedirectToAction("Users");
        }

        foreach (var err in result.Errors)
            ModelState.AddModelError("", err.Description);

        await PopulateUserDropdowns();
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        var roles = await _users.GetRolesAsync(user);
        var vm = new EditUserViewModel
        {
            Id = user.Id, FullName = user.FullName, Email = user.Email ?? "",
            Role = roles.FirstOrDefault() ?? "", ClientId = user.ClientId,
            JobTitle = user.JobTitle, Department = user.Department,
            PhoneNumber = user.PhoneNumber, IsActive = user.IsActive
        };
        await PopulateUserDropdowns();
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid) { await PopulateUserDropdowns(); return View(model); }

        var user = await _users.FindByIdAsync(model.Id);
        if (user == null) return NotFound();

        user.FullName = model.FullName; user.Email = model.Email;
        user.UserName = model.Email; user.ClientId = model.ClientId;
        user.JobTitle = model.JobTitle; user.Department = model.Department;
        user.PhoneNumber = model.PhoneNumber; user.IsActive = model.IsActive;

        await _users.UpdateAsync(user);

        // Update role
        var currentRoles = await _users.GetRolesAsync(user);
        await _users.RemoveFromRolesAsync(user, currentRoles);
        await _users.AddToRoleAsync(user, model.Role);

        await _audit.LogAsync($"Admin updated user {model.Email}, role → {model.Role}", "User", model.Id);
        TempData["Success"] = "User updated.";
        return RedirectToAction("Users");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleUserStatus(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();
        user.IsActive = !user.IsActive;
        await _users.UpdateAsync(user);
        await _audit.LogAsync($"Admin {(user.IsActive ? "activated" : "deactivated")} user {user.Email}", "User", id);
        TempData["Success"] = $"User {(user.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction("Users");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(string id)
    {
        var user = await _users.FindByIdAsync(id);
        if (user == null) return NotFound();

        // Generate a reset link and email it to the user
        var token     = await _users.GeneratePasswordResetTokenAsync(user);
        var resetLink = _tokenUrl.BuildResetLink(user.Email!, token);
        try
        {
            await _email.SendPasswordResetAsync(user.Email!, user.FullName, resetLink);
            user.MustChangePassword = true;
            await _users.UpdateAsync(user);
            await _audit.LogAsync($"Admin sent password reset to {user.Email}", "User", id);
            TempData["Success"] = $"Password reset link sent to {user.Email}.";
        }
        catch
        {
            TempData["Error"] = "Failed to send reset email. Check email settings.";
        }

        return RedirectToAction("Users");
    }

    // ══════════════════════════════════════════════
    // CLIENTS (Hospitals / Clinics)
    // ══════════════════════════════════════════════
    public async Task<IActionResult> Clients(string? search)
    {
        var query = _db.Clients.Include(c => c.Staff).Include(c => c.Patients).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(c => c.Name.Contains(search) || c.RegistrationNo!.Contains(search));

        var clients = await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
        var list = clients.Select(c => new ClientListViewModel
        {
            Id = c.Id, Name = c.Name, Type = c.Type, RegistrationNo = c.RegistrationNo,
            Phone = c.Phone, Email = c.Email, StaffCount = c.Staff.Count,
            PatientCount = c.Patients.Count, IsActive = c.IsActive, CreatedAt = c.CreatedAt
        }).ToList();

        ViewBag.Search = search;
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateClient() => View(new CreateClientViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateClient(CreateClientViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userId = _users.GetUserId(User);
        var client = new Client
        {
            Name = model.Name, Type = model.Type, RegistrationNo = model.RegistrationNo,
            Address = model.Address, Phone = model.Phone, Email = model.Email,
            Website = model.Website, IsActive = true, CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        await _audit.LogAsync($"Admin created client: {model.Name} ({model.Type})", "Client", client.Id.ToString());
        TempData["Success"] = $"Client '{model.Name}' created successfully.";
        return RedirectToAction("Clients");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleClientStatus(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();
        client.IsActive = !client.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Client {(client.IsActive ? "activated" : "deactivated")}.";
        return RedirectToAction("Clients");
    }

    // ══════════════════════════════════════════════
    // SUPPLIERS
    // ══════════════════════════════════════════════
    public async Task<IActionResult> Suppliers(string? search, string? status)
    {
        var query = _db.Suppliers.Include(s => s.ApprovedBy).AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Name.Contains(search) || s.ContactEmail.Contains(search));
        if (status == "pending")
            query = query.Where(s => s.ApprovalStatus == SupplierApprovalStatus.Pending);
        else if (status == "active")
            query = query.Where(s => s.Status == SupplierStatus.Active && s.ApprovalStatus == SupplierApprovalStatus.Approved);

        var suppliers = await query.OrderByDescending(s => s.CreatedAt).ToListAsync();
        var list = suppliers.Select(s => new SupplierListViewModel
        {
            Id = s.Id, SupplierCode = s.SupplierCode, Name = s.Name,
            RegistrationNo = s.RegistrationNo, ContactEmail = s.ContactEmail,
            Phone = s.Phone, BankName = s.BankName, PaymentTerms = s.PaymentTerms,
            Status = s.Status, ApprovalStatus = s.ApprovalStatus,
            ApprovedBy = s.ApprovedBy?.FullName, ApprovedAt = s.ApprovedAt,
            CreatedAt = s.CreatedAt, ContactPersonName = s.ContactPersonName
        }).ToList();

        ViewBag.Search = search;
        ViewBag.StatusFilter = status;
        return View(list);
    }

    [HttpGet]
    public IActionResult CreateSupplier() => View(new CreateSupplierViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSupplier(CreateSupplierViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var count = await _db.Suppliers.CountAsync() + 1;
        var userId = _users.GetUserId(User)!;

        var supplier = new Supplier
        {
            SupplierCode = $"SUP{count:D3}",
            Name = model.Name, RegistrationNo = model.RegistrationNo, TaxNumber = model.TaxNumber,
            ContactEmail = model.ContactEmail, Phone = model.Phone, Address = model.Address,
            PaymentTerms = model.PaymentTerms, BankName = model.BankName,
            BankAccount = model.BankAccount, BankBranch = model.BankBranch,
            ContactPersonName = model.ContactPersonName, ContactPersonEmail = model.ContactPersonEmail,
            ContactPersonPhone = model.ContactPersonPhone, Notes = model.Notes,
            Status = SupplierStatus.Active,
            ApprovalStatus = SupplierApprovalStatus.Approved, // Admin directly approves
            ApprovedByUserId = userId, ApprovedAt = DateTime.UtcNow,
            SubmittedByUserId = userId, CreatedAt = DateTime.UtcNow
        };
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        // Create a portal user for the supplier's contact person
        var portalEmail = model.ContactPersonEmail;
        if (await _users.FindByEmailAsync(portalEmail) == null)
        {
            var portalUser = new ApplicationUser
            {
                UserName = portalEmail, Email = portalEmail,
                FullName = model.ContactPersonName, EmailConfirmed = true,
                IsActive = true, JobTitle = "Supplier Contact",
                CreatedAt = DateTime.UtcNow, MustChangePassword = true
            };
            var tempPassword = $"Supplier@{count:D3}!";
            var result = await _users.CreateAsync(portalUser, tempPassword);
            if (result.Succeeded)
            {
                await _users.AddToRoleAsync(portalUser, "Supplier");
                supplier.PortalUserId = portalUser.Id;
                await _db.SaveChangesAsync();
                try { await _email.SendWelcomeAsync(portalEmail, model.ContactPersonName, tempPassword); }
                catch { /* do not block */ }
                TempData["PortalCredentials"] = $"Supplier portal account created for {model.ContactPersonName}. Login: {portalEmail}";
            }
        }

        await _audit.LogAsync($"Admin created supplier: {model.Name} (code: {supplier.SupplierCode})", "Supplier", supplier.Id.ToString());
        TempData["Success"] = $"Supplier '{model.Name}' created and activated.";
        return RedirectToAction("Suppliers");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSupplier(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        supplier.ApprovalStatus = SupplierApprovalStatus.Approved;
        supplier.ApprovedByUserId = _users.GetUserId(User);
        supplier.ApprovedAt = DateTime.UtcNow;
        supplier.Status = SupplierStatus.Active;
        await _db.SaveChangesAsync();
        await _audit.LogAsync($"Supplier approved: {supplier.Name}", "Supplier", id.ToString());
        TempData["Success"] = $"{supplier.Name} approved and activated.";
        return RedirectToAction("Suppliers");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleSupplierStatus(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        supplier.Status = supplier.Status == SupplierStatus.Active ? SupplierStatus.Suspended : SupplierStatus.Active;
        await _db.SaveChangesAsync();
        await _audit.LogAsync($"Supplier {(supplier.Status == SupplierStatus.Active ? "reactivated" : "suspended")}: {supplier.Name}", "Supplier", id.ToString());
        TempData["Success"] = $"Supplier {(supplier.Status == SupplierStatus.Active ? "reactivated" : "suspended")}.";
        return RedirectToAction("Suppliers");
    }

    // ══════════════════════════════════════════════
    // BLOCKCHAIN / AUDIT
    // ══════════════════════════════════════════════
    public async Task<IActionResult> AuditTrail(int pg = 1, string? search = null)
    {
        const int pageSize = 50;
        var query = _db.AuditLogs.AsQueryable();
        if (!string.IsNullOrEmpty(search))
            query = query.Where(l => l.Action.Contains(search) || l.UserName.Contains(search));
        var total = await query.CountAsync();
        var logs = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((pg - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        ViewBag.Page    = pg;
        ViewBag.PerPage = pageSize;
        ViewBag.Total   = total;
        ViewBag.Search  = search;
        return View(logs);
    }

    public async Task<IActionResult> Blockchain(int pg = 1)
    {
        const int pageSize = 20;
        var total = await _db.BlockchainBlocks.CountAsync();
        var blocks = await _db.BlockchainBlocks
            .OrderByDescending(b => b.BlockIndex)
            .Skip((pg - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        ViewBag.Page       = pg;
        ViewBag.PerPage    = pageSize;
        ViewBag.Total      = total;
        ViewBag.ChainValid = await _blockchain.ValidateChainAsync();
        return View(blocks);
    }

    // ── Helpers
    private async Task PopulateUserDropdowns()
    {
        ViewBag.Roles = DbInitializer.Roles.Select(r => new SelectListItem(r, r)).ToList();
        ViewBag.Clients = (await _db.Clients.Where(c => c.IsActive).OrderBy(c => c.Name).ToListAsync())
            .Select(c => new SelectListItem(c.Name, c.Id.ToString())).ToList();
    }
}
