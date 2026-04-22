using System.ComponentModel.DataAnnotations;

namespace MedBridge.Models;

public enum TicketStatus   { Open, InProgress, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Critical }
public enum TicketCategory { IT, Clinical, Procurement, Finance, General, Other }

public class HelpTicket
{
    public int Id { get; set; }

    [Required, MaxLength(20)]
    public string TicketRef { get; set; } = string.Empty; // TKT-0001

    [Required, MaxLength(300)]
    public string Subject { get; set; } = string.Empty;

    [Required, MaxLength(4000)]
    public string Description { get; set; } = string.Empty;

    public TicketCategory Category { get; set; } = TicketCategory.General;
    public TicketPriority Priority  { get; set; } = TicketPriority.Medium;
    public TicketStatus   Status    { get; set; } = TicketStatus.Open;

    // Raised by
    public string RaisedByUserId { get; set; } = string.Empty;
    public ApplicationUser RaisedBy { get; set; } = null!;
    public DateTime RaisedAt { get; set; } = DateTime.UtcNow;

    // Assigned to (optional)
    public string? AssignedToUserId { get; set; }
    public ApplicationUser? AssignedTo { get; set; }
    public DateTime? AssignedAt { get; set; }

    // Resolution
    [MaxLength(4000)]
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolvedByUserId { get; set; }
    public ApplicationUser? ResolvedBy { get; set; }

    public DateTime? ClosedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<TicketComment> Comments { get; set; } = new List<TicketComment>();
}

public class TicketComment
{
    public int Id { get; set; }

    public int HelpTicketId { get; set; }
    public HelpTicket HelpTicket { get; set; } = null!;

    [Required, MaxLength(4000)]
    public string Body { get; set; } = string.Empty;

    public string AuthorUserId { get; set; } = string.Empty;
    public ApplicationUser Author { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public bool IsInternal { get; set; } = false; // internal notes not shown to ticket raiser
}
