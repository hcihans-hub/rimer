using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RimerApi.Domain.Entities;
using RimerApi.Domain.Enums;
using RimerApi.Infrastructure.Identity;

namespace RimerApi.Infrastructure.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        logger.LogInformation("====================================================");
        logger.LogInformation("DATABASE SEEDING STARTING...");
        logger.LogInformation("====================================================");

        try
        {
            await context.Database.MigrateAsync();
            
            // Seed each part independently so one failure doesn't stop the others
            await TrySeed("Roles", () => SeedRolesAsync(roleManager, logger));
            await TrySeed("Departments", () => SeedDepartmentsAsync(context, logger));
            await TrySeed("Admin User", () => SeedAdminUserAsync(userManager, logger));
            await TrySeed("Guest User", () => SeedGuestUserAsync(userManager, logger));
            await TrySeed("Charts User", () => SeedChartsUserAsync(userManager, logger));
            await TrySeed("Unit Users", () => SeedUnitUsersAsync(userManager, context, logger));
            await TrySeed("Chart Permissions", () => SeedChartPermissionsAsync(context, logger));
            await TrySeed("Demo Tickets", () => SeedDemoTicketsAsync(context, logger));
            await TrySeed("Specific Aged Tickets", () => EnsureAgedTicketsAsync(serviceProvider));
            // await TrySeed("Update Existing for Aging", () => UpdateExistingTicketsForAgingAsync(serviceProvider));
            // await TrySeed("Shuffle Departments", () => ShuffleTicketDepartmentsAsync(serviceProvider));
            // await TrySeed("Set Massive Volumes", () => SetMassiveTicketVolumesAsync(serviceProvider));
            
            logger.LogInformation("DATABASE SEEDING COMPLETED.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "FATAL ERROR DURING DATABASE SEEDING!");
        }
    }

    private static async Task TrySeed(string name, Func<Task> action)
    {
        try { await action(); }
        catch (Exception) { /* Log error if needed, but continue */ }
    }

    private static async Task SeedRolesAsync(RoleManager<IdentityRole<Guid>> roleManager, ILogger logger)
    {
        var roles = new[] { "Admin", "Staff", "Student", "chartsrole", "UnitUser", "Rector" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid> { Id = Guid.NewGuid(), Name = roleName, NormalizedName = roleName.ToUpperInvariant() });
            }
        }
    }

    private static async Task SeedDepartmentsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Departments.AnyAsync()) return;
        var utcNow = DateTime.UtcNow;
        var departments = new List<Department>
        {
            new() { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), Name = "Academic Affairs", IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow },
            new() { Id = Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"), Name = "Student Affairs", IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow },
            new() { Id = Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"), Name = "Administrative Services", IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow },
            new() { Id = Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"), Name = "Information Technology", IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow },
            new() { Id = Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"), Name = "Finance & Budget", IsActive = true, CreatedAt = utcNow, UpdatedAt = utcNow }
        };
        await context.Departments.AddRangeAsync(departments);
        await context.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string email = "admin@rimer.edu";
        if (await userManager.FindByEmailAsync(email) is not null) return;
        var user = new ApplicationUser { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), FirstName = "System", LastName = "Admin", UserName = email, Email = email, EmailConfirmed = true, Role = UserRole.Admin };
        await userManager.CreateAsync(user, "Admin@123456");
        await userManager.AddToRoleAsync(user, "Admin");
    }

    private static async Task SeedGuestUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string email = "test@test.com";
        if (await userManager.FindByEmailAsync(email) is not null) return;
        var user = new ApplicationUser { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), FirstName = "Test", LastName = "User", UserName = email, Email = email, EmailConfirmed = true, Role = UserRole.Student };
        await userManager.CreateAsync(user, "Password123!");
        await userManager.AddToRoleAsync(user, "Student");
    }

    private static async Task SeedChartsUserAsync(UserManager<ApplicationUser> userManager, ILogger logger)
    {
        const string email = "rector@rimer.edu";
        // Check both by email and id to be absolutely sure
        var existing = await userManager.FindByEmailAsync(email);
        if (existing != null) return;

        var user = new ApplicationUser 
        { 
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), 
            FirstName = "Rector", 
            LastName = "Analytics", 
            UserName = email, 
            Email = email, 
            EmailConfirmed = true, 
            Role = UserRole.Rector 
        };

        var result = await userManager.CreateAsync(user, "Rector@123456");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "chartsrole");
            logger.LogInformation(">>> SEEDED CHARTS USER: {Email}", email);
        }
        else
        {
            logger.LogError(">>> FAILED TO SEED CHARTS USER: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private static async Task SeedChartPermissionsAsync(ApplicationDbContext context, ILogger logger)
    {
        var rectorId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Skip if rector already has permissions
        if (context.UserChartPermissions.Any(p => p.UserId == rectorId))
            return;

        var utcNow = DateTime.UtcNow;

        foreach (var key in Enum.GetValues<ChartKey>())
        {
            context.UserChartPermissions.Add(new UserChartPermission
            {
                Id = Guid.NewGuid(),
                UserId = rectorId,
                ChartKey = key,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        await context.SaveChangesAsync();
        logger.LogInformation(">>> SEEDED CHART PERMISSIONS for rector user (all {Count} charts)", Enum.GetValues<ChartKey>().Length);
    }

    private static async Task SeedDemoTicketsAsync(ApplicationDbContext context, ILogger logger)
    {
        if (await context.Tickets.AnyAsync()) return;

        var creatorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var deptIds = new[]
        {
            Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), // Academic Affairs
            Guid.Parse("b2c3d4e5-f6a7-8901-bcde-f12345678901"), // Student Affairs
            Guid.Parse("c3d4e5f6-a7b8-9012-cdef-123456789012"), // Administrative Services
            Guid.Parse("d4e5f6a7-b8c9-0123-defa-234567890123"), // Information Technology
            Guid.Parse("e5f6a7b8-c9d0-1234-efab-345678901234"), // Finance & Budget
        };

        var utcNow = DateTime.UtcNow;
        var rng = new Random(42); // deterministic seed
        var tickets = new List<Ticket>();
        var seq = 1;

        // Helper to create a ticket
        void Add(string title, TicketCategory cat, TicketStatus status, int deptIdx, int daysAgo)
        {
            tickets.Add(new Ticket
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = $"Demo ticket: {title}",
                ReferenceNo = $"RIM-2026-{seq:D4}",
                Category = cat,
                Status = status,
                Priority = (TicketPriority)rng.Next(1, 4),
                CreatorId = creatorId,
                DepartmentId = deptIds[deptIdx],
                TermsAccepted = true,
                CreatedAt = utcNow.AddDays(-daysAgo),
                UpdatedAt = utcNow.AddDays(-rng.Next(0, daysAgo + 1))
            });
            seq++;
        }

        // ── Complaints (8) ─────────────────────────────────────────
        Add("Yemekhane hijyen sorunu", TicketCategory.Complaint, TicketStatus.Closed, 2, 120);
        Add("Kütüphane klima arızası", TicketCategory.Complaint, TicketStatus.Submitted, 2, 95);
        Add("Sınav programı çakışması", TicketCategory.Complaint, TicketStatus.Reviewing, 0, 30);
        Add("Otopark yetersizliği", TicketCategory.Complaint, TicketStatus.Submitted, 2, 200);
        Add("Dersliklerde internet kesilmesi", TicketCategory.Complaint, TicketStatus.Closed, 3, 15);
        Add("Kantin fiyatları yüksek", TicketCategory.Complaint, TicketStatus.Submitted, 4, 60);
        Add("Laboratuvar ekipman eksikliği", TicketCategory.Complaint, TicketStatus.Reviewing, 0, 45);
        Add("Asansör arızası bina-C", TicketCategory.Complaint, TicketStatus.Submitted, 2, 380);

        // ── Suggestions (6) ────────────────────────────────────────
        Add("Online randevu sistemi önerisi", TicketCategory.Suggestion, TicketStatus.Closed, 3, 50);
        Add("Kampüs bisiklet paylaşım sistemi", TicketCategory.Suggestion, TicketStatus.Submitted, 2, 25);
        Add("Ders değerlendirme anketi iyileştirme", TicketCategory.Suggestion, TicketStatus.Reviewing, 0, 70);
        Add("Mobil uygulama geliştirme", TicketCategory.Suggestion, TicketStatus.Submitted, 3, 10);
        Add("Yeşil kampüs projesi", TicketCategory.Suggestion, TicketStatus.Closed, 2, 90);
        Add("Spor tesisleri genişletme", TicketCategory.Suggestion, TicketStatus.Submitted, 1, 55);

        // ── Requests (8) ───────────────────────────────────────────
        Add("Transkript talebi", TicketCategory.Request, TicketStatus.Closed, 1, 5);
        Add("Burs başvuru durumu", TicketCategory.Request, TicketStatus.Reviewing, 4, 20);
        Add("Yurt değişikliği talebi", TicketCategory.Request, TicketStatus.Submitted, 1, 75);
        Add("Staj belgesi onayı", TicketCategory.Request, TicketStatus.Closed, 0, 12);
        Add("Ek ders açılması talebi", TicketCategory.Request, TicketStatus.Reviewing, 0, 40);
        Add("Laboratuvar erişim izni", TicketCategory.Request, TicketStatus.Submitted, 3, 100);
        Add("Mezuniyet töreni bilgi talebi", TicketCategory.Request, TicketStatus.Closed, 1, 8);
        Add("Kütüphane üyelik yenileme", TicketCategory.Request, TicketStatus.Closed, 1, 3);

        // ── Thanks (5) ─────────────────────────────────────────────
        Add("Hızlı belge onayı için teşekkür", TicketCategory.Thanks, TicketStatus.Closed, 1, 7);
        Add("IT ekibine teşekkür", TicketCategory.Thanks, TicketStatus.Closed, 3, 14);
        Add("Danışman hocama teşekkürler", TicketCategory.Thanks, TicketStatus.Closed, 0, 22);
        Add("Temizlik personeline teşekkür", TicketCategory.Thanks, TicketStatus.Closed, 2, 35);
        Add("Burs onayı için teşekkür", TicketCategory.Thanks, TicketStatus.Closed, 4, 18);

        // ── Info Requests (5) ──────────────────────────────────────
        Add("Kayıt dondurma prosedürü", TicketCategory.InfoRequest, TicketStatus.Closed, 1, 28);
        Add("Yaz okulu başvuru tarihleri", TicketCategory.InfoRequest, TicketStatus.Reviewing, 0, 6);
        Add("Erasmus başvuru koşulları", TicketCategory.InfoRequest, TicketStatus.Submitted, 0, 48);
        Add("Harç ödeme takvimi", TicketCategory.InfoRequest, TicketStatus.Closed, 4, 33);
        Add("Çift anadal programı bilgisi", TicketCategory.InfoRequest, TicketStatus.Submitted, 0, 185);

        // ── Questions (3) ──────────────────────────────────────────
        Add("Ders ekleme/bırakma tarihi", TicketCategory.Question, TicketStatus.Closed, 0, 9);
        Add("Kampüs WiFi şifresi nedir?", TicketCategory.Question, TicketStatus.Closed, 3, 2);
        Add("Mezuniyet koşulları hakkında", TicketCategory.Question, TicketStatus.Submitted, 0, 65);

        // ── Aging Analysis Specific (3 new ones) ───────────────────
        Add("45 Günü Geçmiş Talep (SLA)", TicketCategory.Complaint, TicketStatus.Reviewing, 2, 46);
        Add("90 Günü Geçmiş Talep (Kritik)", TicketCategory.Request, TicketStatus.Reviewing, 3, 91);
        Add("180 Günü Geçmiş Talep (Arşivlik)", TicketCategory.Complaint, TicketStatus.Reviewing, 0, 181);

        await context.Tickets.AddRangeAsync(tickets);
        await context.SaveChangesAsync();
        logger.LogInformation(">>> SEEDED {Count} DEMO TICKETS", tickets.Count);
    }

    /// <summary>
    /// Ensures specific tickets for aging analysis exist, regardless of other data.
    /// </summary>
    public static async Task EnsureAgedTicketsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        if (await context.Tickets.AnyAsync(t => t.Title.Contains("(SLA)") || t.Title.Contains("(Kritik)")))
            return;

        var creatorId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var utcNow = DateTime.UtcNow;
        
        var agedTickets = new List<Ticket>
        {
            new() { 
                Id = Guid.NewGuid(), Title = "45 Günü Geçmiş Talep (SLA)", Description = "Yaşlandırma analizi testi", 
                ReferenceNo = "RIM-OLD-0045", Category = TicketCategory.Complaint, Status = TicketStatus.Reviewing,
                CreatorId = creatorId, CreatedAt = utcNow.AddDays(-46), UpdatedAt = utcNow.AddDays(-46)
            },
            new() { 
                Id = Guid.NewGuid(), Title = "90 Günü Geçmiş Talep (Kritik)", Description = "Yaşlandırma analizi testi", 
                ReferenceNo = "RIM-OLD-0090", Category = TicketCategory.Request, Status = TicketStatus.Reviewing,
                CreatorId = creatorId, CreatedAt = utcNow.AddDays(-91), UpdatedAt = utcNow.AddDays(-91)
            },
            new() { 
                Id = Guid.NewGuid(), Title = "180 Günü Geçmiş Talep (Arşivlik)", Description = "Yaşlandırma analizi testi", 
                ReferenceNo = "RIM-OLD-0180", Category = TicketCategory.Complaint, Status = TicketStatus.Reviewing,
                CreatorId = creatorId, CreatedAt = utcNow.AddDays(-181), UpdatedAt = utcNow.AddDays(-181)
            }
        };

        await context.Tickets.AddRangeAsync(agedTickets);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Updates 3 existing non-closed tickets to be old, ensuring aging analysis shows data.
    /// </summary>
    public static async Task UpdateExistingTicketsForAgingAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var utcNow = DateTime.UtcNow;
        
        // Find 3 tickets that are not closed
        var ticketsToUpdate = await context.Tickets
            .Where(t => t.Status != TicketStatus.Closed)
            .Take(3)
            .ToListAsync();

        if (ticketsToUpdate.Count >= 1)
        {
            ticketsToUpdate[0].CreatedAt = utcNow.AddDays(-50);
            ticketsToUpdate[0].Title += " (Aging 45+)";
        }
        if (ticketsToUpdate.Count >= 2)
        {
            ticketsToUpdate[1].CreatedAt = utcNow.AddDays(-100);
            ticketsToUpdate[1].Title += " (Aging 90+)";
        }
        if (ticketsToUpdate.Count >= 3)
        {
            ticketsToUpdate[2].CreatedAt = utcNow.AddDays(-200);
            ticketsToUpdate[2].Title += " (Aging 180+)";
        }

        if (ticketsToUpdate.Any())
        {
            await context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Randomly distributes existing tickets to different departments for testing rankings.
    /// </summary>
    public static async Task ShuffleTicketDepartmentsAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var tickets = await context.Tickets.ToListAsync();
        var depts = await context.Departments.ToListAsync();
        
        if (!tickets.Any() || !depts.Any()) return;

        var rng = new Random();
        foreach (var ticket in tickets)
        {
            // Pick a random department
            var randomDept = depts[rng.Next(depts.Count)];
            ticket.AssignedDepartmentId = randomDept.Id;
            ticket.DepartmentId = randomDept.Id; // Sync both for clean data
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Sets specific massive volumes for testing chart rendering:
    /// 1000 Resolved, 20000 Unanswered (Older than 45 days), others Pending.
    /// </summary>
    public static async Task SetMassiveTicketVolumesAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        var tickets = await context.Tickets.ToListAsync();
        if (tickets.Count < 21000) return; // Only run if we have enough data

        var utcNow = DateTime.UtcNow;
        var rng = new Random();

        for (int i = 0; i < tickets.Count; i++)
        {
            if (i < 1000)
            {
                tickets[i].Status = TicketStatus.Closed;
            }
            else if (i < 21000)
            {
                tickets[i].Status = TicketStatus.Submitted;
                // Distribute creation dates over the last 6 months for the trend chart
                tickets[i].CreatedAt = utcNow.AddMonths(-rng.Next(0, 6)).AddDays(-rng.Next(1, 28));
            }
            else
            {
                tickets[i].Status = TicketStatus.Reviewing;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitUsersAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context, ILogger logger)
    {
        // 1. Information Technology
        var itDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Information Technology" || d.Name == "Bilgi İşlem");
        if (itDept == null)
        {
            itDept = new Department { Id = Guid.NewGuid(), Name = "Bilgi İşlem", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await context.Departments.AddAsync(itDept);
        }

        // 2. İletişim
        var commDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "İletişim" || d.Name == "İletişim Daire Başkanlığı");
        if (commDept == null)
        {
            commDept = new Department { Id = Guid.NewGuid(), Name = "İletişim", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await context.Departments.AddAsync(commDept);
        }

        // 3. Rektörlük
        var recDept = await context.Departments.FirstOrDefaultAsync(d => d.Name == "Rektörlük" || d.Name == "Rektörlük Birimi");
        if (recDept == null)
        {
            recDept = new Department { Id = Guid.NewGuid(), Name = "Rektörlük", IsActive = true, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            await context.Departments.AddAsync(recDept);
        }

        await context.SaveChangesAsync();

        await EnsureUnitUserAsync(userManager, "bilgiislem@ankara.edu", "Bilgi İşlem", "Birim Yetkilisi", itDept.Id, logger);
        await EnsureUnitUserAsync(userManager, "iletisim@ankara.edu", "İletişim", "Birim Yetkilisi", commDept.Id, logger);
        await EnsureUnitUserAsync(userManager, "rektorlukbirimi@ankara.edu", "Rektörlük", "Birim Yetkilisi", recDept.Id, logger);
    }

    private static async Task EnsureUnitUserAsync(UserManager<ApplicationUser> userManager, string email, string firstName, string lastName, Guid departmentId, ILogger logger)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                FirstName = firstName,
                LastName = lastName,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                Role = UserRole.UnitUser,
                DepartmentId = departmentId,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var result = await userManager.CreateAsync(user, "Password123!");
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, "UnitUser");
                logger.LogInformation(">>> SEEDED UNIT USER: {Email}", email);
            }
            else
            {
                logger.LogError(">>> FAILED TO SEED UNIT USER: {Email}, Errors: {Errors}", email, string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
        else
        {
            user.Role = UserRole.UnitUser;
            user.DepartmentId = departmentId;
            user.FirstName = firstName;
            user.LastName = lastName;
            await userManager.UpdateAsync(user);

            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, "UnitUser");

            var token = await userManager.GeneratePasswordResetTokenAsync(user);
            var resetResult = await userManager.ResetPasswordAsync(user, token, "Password123!");
            if (resetResult.Succeeded)
            {
                logger.LogInformation(">>> RESET PASSWORD AND ROLE FOR UNIT USER: {Email}", email);
            }
            else
            {
                logger.LogError(">>> FAILED TO RESET PASSWORD FOR UNIT USER: {Email}, Errors: {Errors}", email, string.Join(", ", resetResult.Errors.Select(e => e.Description)));
            }
        }
    }
}
