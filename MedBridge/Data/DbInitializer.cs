using MedBridge.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MedBridge.Data;

public static class DbInitializer
{
    public static readonly string[] Roles = new[]
    {
        "Admin",
        "Doctor",
        "Nurse",
        "Pharmacist",
        "ProcurementOfficer",
        "HeadOfProcurement",
        "HeadOfSupplyChain",
        "FinanceManager",
        "ReceivingClerk",
        "Auditor",
        "Supplier"
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        try
        {
            await db.Database.MigrateAsync();

            // Seed roles
            foreach (var role in Roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                    logger.LogInformation("Created role: {Role}", role);
                }
            }

            // Seed admin user
            const string adminEmail = "Mamishi.Madire@admin";
            const string adminPassword = "Admin123@";

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Mamishi Madire",
                    EmailConfirmed = true,
                    IsActive = true,
                    JobTitle = "System Administrator",
                    CreatedAt = DateTime.UtcNow
                };
                var result = await userManager.CreateAsync(admin, adminPassword);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Admin user created: {Email}", adminEmail);
                }
                else
                {
                    logger.LogError("Failed to create admin: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            // Seed sample suppliers
            if (!db.Suppliers.Any())
            {
                var suppliers = new List<Supplier>
                {
                    new() { SupplierCode = "SUP001", Name = "Adcock Ingram", RegistrationNo = "1893/000010/06", TaxNumber = "4010010010", ContactEmail = "orders@adcock.co.za", Phone = "011 635 0000", Address = "Bryanston, Johannesburg", AccountNumber = "AI-2024-001", PaymentTerms = "30 days", BankName = "Standard Bank", BankAccount = "01-2345-678", BankBranch = "051-001", Status = SupplierStatus.Active, ApprovalStatus = SupplierApprovalStatus.Approved, ApprovedAt = DateTime.UtcNow, ContactPersonName = "John Adcock", ContactPersonEmail = "orders@adcock.co.za" },
                    new() { SupplierCode = "SUP002", Name = "Cipla SA", RegistrationNo = "2000/028425/07", TaxNumber = "4200123456", ContactEmail = "procurement@cipla.co.za", Phone = "011 451 0000", Address = "Midrand, Johannesburg", AccountNumber = "CI-2024-002", PaymentTerms = "30 days", BankName = "ABSA", BankAccount = "40-5566-778", BankBranch = "632-005", Status = SupplierStatus.Active, ApprovalStatus = SupplierApprovalStatus.Approved, ApprovedAt = DateTime.UtcNow, ContactPersonName = "Priya Naidoo", ContactPersonEmail = "procurement@cipla.co.za" },
                    new() { SupplierCode = "SUP003", Name = "Aspen SA", RegistrationNo = "1985/002935/06", TaxNumber = "9100007654", ContactEmail = "orders@aspen.co.za", Phone = "031 580 8888", Address = "Durban, KwaZulu-Natal", AccountNumber = "AS-2024-006", PaymentTerms = "30 days", BankName = "Standard Bank", BankAccount = "05-6789-012", BankBranch = "052-156", Status = SupplierStatus.Active, ApprovalStatus = SupplierApprovalStatus.Approved, ApprovedAt = DateTime.UtcNow, ContactPersonName = "Thabo Nkosi", ContactPersonEmail = "orders@aspen.co.za" }
                };
                db.Suppliers.AddRange(suppliers);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} suppliers", suppliers.Count);
            }

            // Seed sample medications
            if (!db.Medications.Any())
            {
                var sup1 = db.Suppliers.First(s => s.SupplierCode == "SUP001");
                var sup2 = db.Suppliers.First(s => s.SupplierCode == "SUP002");
                var medications = new List<Medication>
                {
                    new() { Code = "MED001", Name = "Metformin 500mg",               BatchNumber = "B2024001", ExpiryDate = new DateTime(2026, 8, 1),  Quantity = 240, MinimumQuantity = 50, Unit = "tablets",  SupplierId = sup1.Id, UnitCost = 4.20m },
                    new() { Code = "MED002", Name = "Amlodipine 5mg",                BatchNumber = "B2024002", ExpiryDate = new DateTime(2026, 12, 15), Quantity = 30,  MinimumQuantity = 40, Unit = "tablets",  SupplierId = sup2.Id, UnitCost = 6.80m },
                    new() { Code = "MED003", Name = "Eltroxin 50mcg",                BatchNumber = "B2024003", ExpiryDate = new DateTime(2025, 6, 30),  Quantity = 180, MinimumQuantity = 30, Unit = "tablets",  SupplierId = sup1.Id, UnitCost = 3.50m },
                    new() { Code = "MED004", Name = "Salbutamol Inhaler 100mcg",     BatchNumber = "B2024004", ExpiryDate = new DateTime(2026, 3, 20),  Quantity = 15,  MinimumQuantity = 10, Unit = "inhalers", SupplierId = sup2.Id, UnitCost = 85.00m },
                    new() { Code = "MED005", Name = "Amoxicillin 500mg",             BatchNumber = "B2024005", ExpiryDate = new DateTime(2025, 4, 10),  Quantity = 8,   MinimumQuantity = 20, Unit = "capsules", SupplierId = sup1.Id, UnitCost = 2.10m },
                    new() { Code = "MED006", Name = "Atorvastatin 20mg",             BatchNumber = "B2024006", ExpiryDate = new DateTime(2027, 1, 1),   Quantity = 120, MinimumQuantity = 30, Unit = "tablets",  SupplierId = sup2.Id, UnitCost = 9.40m },
                    new() { Code = "MED007", Name = "Prednisone 5mg",                BatchNumber = "B2024007", ExpiryDate = new DateTime(2026, 9, 1),   Quantity = 60,  MinimumQuantity = 20, Unit = "tablets",  SupplierId = sup1.Id, UnitCost = 1.80m },
                    new() { Code = "MED008", Name = "Ciprofloxacin 500mg",           BatchNumber = "B2024008", ExpiryDate = new DateTime(2026, 5, 1),   Quantity = 0,   MinimumQuantity = 20, Unit = "tablets",  SupplierId = sup2.Id, UnitCost = 12.00m }
                };
                db.Medications.AddRange(medications);
                await db.SaveChangesAsync();
                logger.LogInformation("Seeded {Count} medications", medications.Count);
            }

            // Seed genesis blockchain block
            if (!db.BlockchainBlocks.Any())
            {
                var genesis = new BlockchainBlock
                {
                    BlockIndex = 0,
                    PreviousHash = "0",
                    Timestamp = DateTime.UtcNow,
                    Data = "{\"event\":\"genesis\"}",
                    UserId = "system",
                    UserName = "System",
                    Role = "Admin",
                    Action = "MedBridge blockchain genesis block — system initialised",
                    Nonce = 1
                };
                genesis.Hash = ComputeGenesisHash(genesis);
                db.BlockchainBlocks.Add(genesis);
                await db.SaveChangesAsync();
                logger.LogInformation("Genesis block created: {Hash}", genesis.Hash);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during database seeding");
            throw;
        }
    }

    private static string ComputeGenesisHash(BlockchainBlock block)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var raw = $"{block.BlockIndex}{block.PreviousHash}{block.Timestamp:O}{block.Data}{block.UserId}{block.Action}{block.Nonce}";
        var bytes = sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLower();
    }
}
