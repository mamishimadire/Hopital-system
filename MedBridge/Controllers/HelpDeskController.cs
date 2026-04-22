using MedBridge.Data;
using MedBridge.Models;
using MedBridge.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Controllers;

[Authorize]
public class HelpDeskController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly IEmailService _email;
    private readonly IAuditService _audit;

    public HelpDeskController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        IEmailService email, IAuditService audit)
    {
        _db    = db;
        _users = users;
        _email = email;
        _audit = audit;
    }

    // ── Ticket List ────────────────────────────────────────────────────────
    public async Task<IActionResult> Index(string? status, string? priority, string? search)
    {
        var user   = await _users.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        var query = _db.HelpTickets
            .Include(t => t.RaisedBy)
            .Include(t => t.AssignedTo)
            .AsQueryable();

        // Non-admins only see their own tickets
        if (!isAdmin)
            query = query.Where(t => t.RaisedByUserId == user!.Id);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t => t.Subject.Contains(search) || t.TicketRef.Contains(search));

        if (Enum.TryParse<TicketStatus>(status, true, out var s))
            query = query.Where(t => t.Status == s);

        if (Enum.TryParse<TicketPriority>(priority, true, out var p))
            query = query.Where(t => t.Priority == p);

        var tickets = await query.OrderByDescending(t => t.RaisedAt).ToListAsync();

        ViewBag.Search   = search;
        ViewBag.Status   = status;
        ViewBag.Priority = priority;
        ViewBag.IsAdmin  = isAdmin;
        ViewData["Title"] = "Help Desk";
        return View(tickets);
    }

    // ── View Ticket ────────────────────────────────────────────────────────
    public async Task<IActionResult> Details(int id)
    {
        var user = await _users.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _db.HelpTickets
            .Include(t => t.RaisedBy)
            .Include(t => t.AssignedTo)
            .Include(t => t.ResolvedBy)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null) return NotFound();

        // Only raiser or admins can view
        if (!isAdmin && ticket.RaisedByUserId != user!.Id)
            return Forbid();

        if (isAdmin)
            ViewBag.AgentList = (await _users.GetUsersInRoleAsync("Admin"))
                .Select(u => new SelectListItem(u.FullName, u.Id)).ToList();

        ViewBag.IsAdmin = isAdmin;
        ViewData["Title"] = $"Ticket {ticket.TicketRef}";
        return View(ticket);
    }

    // ── Raise New Ticket ───────────────────────────────────────────────────
    [HttpGet]
    public IActionResult Create()
    {
        ViewData["Title"] = "Raise Support Ticket";
        return View(new HelpTicket());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(HelpTicket model)
    {
        ModelState.Remove("RaisedBy");
        ModelState.Remove("RaisedByUserId");
        ModelState.Remove("TicketRef");
        if (!ModelState.IsValid) return View(model);

        var user  = await _users.GetUserAsync(User);
        var count = await _db.HelpTickets.CountAsync() + 1;

        model.TicketRef      = $"TKT-{count:D4}";
        model.RaisedByUserId = user!.Id;
        model.RaisedAt       = DateTime.UtcNow;
        model.UpdatedAt      = DateTime.UtcNow;
        model.Status         = TicketStatus.Open;

        _db.HelpTickets.Add(model);
        await _db.SaveChangesAsync();

        await _audit.LogAsync($"Help ticket raised: {model.TicketRef} — {model.Subject}", "HelpTicket", model.Id.ToString());

        // Notify the raiser
        try { await _email.SendHelpTicketNotificationAsync(user.Email!, user.FullName, model.TicketRef, model.Subject); }
        catch { /* non-blocking */ }

        TempData["Success"] = $"Ticket {model.TicketRef} submitted. We'll be in touch soon.";
        return RedirectToAction("Index");
    }

    // ── Add Comment ────────────────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddComment(int ticketId, string body, bool isInternal = false)
    {
        var user = await _users.GetUserAsync(User);
        var isAdmin = User.IsInRole("Admin");

        var ticket = await _db.HelpTickets.Include(t => t.RaisedBy).FirstOrDefaultAsync(t => t.Id == ticketId);
        if (ticket == null) return NotFound();
        if (!isAdmin && ticket.RaisedByUserId != user!.Id) return Forbid();

        if (string.IsNullOrWhiteSpace(body))
        {
            TempData["Error"] = "Comment cannot be empty.";
            return RedirectToAction("Details", new { id = ticketId });
        }

        var comment = new TicketComment
        {
            HelpTicketId = ticketId,
            Body         = body.Trim(),
            AuthorUserId = user!.Id,
            CreatedAt    = DateTime.UtcNow,
            IsInternal   = isInternal && isAdmin
        };
        _db.TicketComments.Add(comment);
        ticket.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        // Notify raiser of update (if comment is from an admin/agent)
        if (isAdmin && !comment.IsInternal && ticket.RaisedBy?.Email != null)
        {
            try { await _email.SendHelpTicketNotificationAsync(ticket.RaisedBy.Email, ticket.RaisedBy.FullName, ticket.TicketRef, ticket.Subject, isUpdate: true); }
            catch { /* non-blocking */ }
        }

        return RedirectToAction("Details", new { id = ticketId });
    }

    // ── Update Status (Admin) ──────────────────────────────────────────────
    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, TicketStatus status, string? resolution, string? assignedToUserId)
    {
        var admin  = await _users.GetUserAsync(User);
        var ticket = await _db.HelpTickets.Include(t => t.RaisedBy).FirstOrDefaultAsync(t => t.Id == id);
        if (ticket == null) return NotFound();

        ticket.Status    = status;
        ticket.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(assignedToUserId))
        {
            ticket.AssignedToUserId = assignedToUserId;
            ticket.AssignedAt       = DateTime.UtcNow;
        }

        if (status == TicketStatus.Resolved && !string.IsNullOrEmpty(resolution))
        {
            ticket.Resolution      = resolution;
            ticket.ResolvedAt      = DateTime.UtcNow;
            ticket.ResolvedByUserId = admin!.Id;
        }

        if (status == TicketStatus.Closed)
            ticket.ClosedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        await _audit.LogAsync($"Ticket {ticket.TicketRef} status → {status}", "HelpTicket", id.ToString());

        // Notify raiser
        if (ticket.RaisedBy?.Email != null)
        {
            try { await _email.SendHelpTicketNotificationAsync(ticket.RaisedBy.Email, ticket.RaisedBy.FullName, ticket.TicketRef, ticket.Subject, isUpdate: true); }
            catch { /* non-blocking */ }
        }

        TempData["Success"] = $"Ticket {ticket.TicketRef} updated.";
        return RedirectToAction("Details", new { id });
    }
}
