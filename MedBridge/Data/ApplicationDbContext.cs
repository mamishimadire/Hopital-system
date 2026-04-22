using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MedBridge.Models;

namespace MedBridge.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }

    // Core EMR
    public DbSet<Client> Clients { get; set; }
    public DbSet<Patient> Patients { get; set; }
    public DbSet<PatientCondition> PatientConditions { get; set; }
    public DbSet<PatientAllergy> PatientAllergies { get; set; }
    public DbSet<PatientVitals> PatientVitals { get; set; }
    public DbSet<PatientScreening> PatientScreenings { get; set; }
    public DbSet<PatientSurgery> PatientSurgeries { get; set; }
    public DbSet<PatientNote> PatientNotes { get; set; }

    // Medication
    public DbSet<Medication> Medications { get; set; }
    public DbSet<Prescription> Prescriptions { get; set; }

    // Suppliers
    public DbSet<Supplier> Suppliers { get; set; }
    public DbSet<SupplierDocument> SupplierDocuments { get; set; }

    // Procurement
    public DbSet<PurchaseRequisition> PurchaseRequisitions { get; set; }
    public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; set; }
    public DbSet<Invoice> Invoices { get; set; }

    // Finance
    public DbSet<FinanceInboxItem> FinanceInboxItems { get; set; }

    // Help Desk
    public DbSet<HelpTicket> HelpTickets { get; set; }
    public DbSet<TicketComment> TicketComments { get; set; }

    // Blockchain / Audit
    public DbSet<BlockchainBlock> BlockchainBlocks { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(e =>
        {
            e.HasOne(u => u.Client).WithMany(c => c.Staff).HasForeignKey(u => u.ClientId).OnDelete(DeleteBehavior.SetNull);
        });

        builder.Entity<Prescription>(e =>
        {
            e.HasOne(p => p.PrescribedBy).WithMany().HasForeignKey(p => p.PrescribedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(p => p.DispensedBy).WithMany().HasForeignKey(p => p.DispensedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseRequisition>(e =>
        {
            e.HasOne(r => r.CreatedBy).WithMany().HasForeignKey(r => r.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(r => r.ApprovedBy).WithMany().HasForeignKey(r => r.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PurchaseOrder>(e =>
        {
            e.HasOne(o => o.CreatedBy).WithMany().HasForeignKey(o => o.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.ApprovedBy).WithMany().HasForeignKey(o => o.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.SentBy).WithMany().HasForeignKey(o => o.SentByUserId).OnDelete(DeleteBehavior.Restrict);
            // PO → PR: explicit FK on PO side, reverse navigation on PR side
            e.HasOne(o => o.PurchaseRequisition).WithOne(r => r.PurchaseOrder)
             .HasForeignKey<PurchaseOrder>(o => o.PurchaseRequisitionId)
             .IsRequired(false).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(o => o.Invoice).WithOne(i => i.PurchaseOrder).HasForeignKey<Invoice>(i => i.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<GoodsReceiptNote>(e =>
        {
            e.HasOne(g => g.ReceivedBy).WithMany().HasForeignKey(g => g.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Invoice>(e =>
        {
            e.HasOne(i => i.UploadedBy).WithMany().HasForeignKey(i => i.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(i => i.PaidBy).WithMany().HasForeignKey(i => i.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<Supplier>(e =>
        {
            e.HasOne(s => s.ApprovedBy).WithMany().HasForeignKey(s => s.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(s => s.SubmittedBy).WithMany().HasForeignKey(s => s.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<PatientNote>(e =>
        {
            e.HasOne(n => n.CreatedBy).WithMany().HasForeignKey(n => n.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<HelpTicket>(e =>
        {
            e.HasOne(t => t.RaisedBy).WithMany().HasForeignKey(t => t.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.AssignedTo).WithMany().HasForeignKey(t => t.AssignedToUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(t => t.ResolvedBy).WithMany().HasForeignKey(t => t.ResolvedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TicketComment>(e =>
        {
            e.HasOne(c => c.Author).WithMany().HasForeignKey(c => c.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.HelpTicket).WithMany(t => t.Comments).HasForeignKey(c => c.HelpTicketId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
