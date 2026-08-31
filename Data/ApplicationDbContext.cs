using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SchoolSystemAPI.Models;
using System.Security.Claims;
using System.Text.Json;

namespace SchoolSystemAPI.Data;

public class ApplicationDbContext : DbContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IHttpContextAccessor httpContextAccessor = null) : base(options) 
    { 
        _httpContextAccessor = httpContextAccessor;
    }

    public DbSet<AppUser> Users { get; set; }
    public DbSet<ClassRoom> ClassRooms { get; set; }
    public DbSet<Student> Students { get; set; }
    public DbSet<StudentGrade> StudentGrades { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<SubjectConfiguration> SubjectConfigurations { get; set; }
    public DbSet<AppSetting> AppSettings { get; set; }
    public DbSet<StageFee> StageFees { get; set; }
    public DbSet<StudentArchive> StudentArchives { get; set; }
    public DbSet<Excuse> Excuses { get; set; }
    public DbSet<SubscriptionPayment> SubscriptionPayments { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<PendingRegistration> PendingRegistrations { get; set; }
    public DbSet<Family> Families { get; set; }

    public DbSet<TarbeyaFamily> TarbeyaFamilies { get; set; }
    public DbSet<TarbeyaStage> TarbeyaStages { get; set; }
    public DbSet<TarbeyaClass> TarbeyaClasses { get; set; }
    public DbSet<TarbeyaStudent> TarbeyaStudents { get; set; }
    public DbSet<TarbeyaAttendance> TarbeyaAttendances { get; set; }
    public DbSet<TarbeyaVisitationRecord> TarbeyaVisitationRecords { get; set; }
    public DbSet<TarbeyaPointTransaction> TarbeyaPointTransactions { get; set; }
    public DbSet<TarbeyaArea> TarbeyaAreas { get; set; }
    
    // Trips & Financials
    public DbSet<TarbeyaTrip> TarbeyaTrips { get; set; }
    public DbSet<TarbeyaTripSubscription> TarbeyaTripSubscriptions { get; set; }
    public DbSet<TarbeyaTripExpense> TarbeyaTripExpenses { get; set; }

    // Family Finances
    public DbSet<TarbeyaFamilyTransaction> TarbeyaFamilyTransactions { get; set; }

    // Servants Management
    public DbSet<ServantAttendance> ServantAttendances { get; set; }
    public DbSet<ServiceTask> ServiceTasks { get; set; }
    
    // Notifications & Spiritual Tracking
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<TarbeyaLiturgyAttendance> TarbeyaLiturgyAttendances { get; set; }

    // Mahragan & Summer Clubs
    public DbSet<MahraganEvent> MahraganEvents { get; set; }
    public DbSet<MahraganCompetition> MahraganCompetitions { get; set; }
    public DbSet<MahraganEnrollment> MahraganEnrollments { get; set; }
    public DbSet<MahraganScore> MahraganScores { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ==========================================
        // 1. AppUser Configuration
        // ==========================================
        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.HasIndex(e => e.Username).IsUnique(); // Ù„Ù…Ù†Ø¹ Ø§Ù„ØªÙƒØ±Ø§Ø±
            entity.Property(e => e.Email).HasMaxLength(150);
            entity.Property(e => e.Role).HasMaxLength(20).HasDefaultValue("User");
            
            // Relation with ClassRoom (Optional)
            entity.HasOne(e => e.ClassRoom)
                  .WithMany(c => c.SupervisedByUsers)
                  .HasForeignKey(e => e.ClassRoomId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // 2. ClassRoom Configuration
        // ==========================================
        modelBuilder.Entity<ClassRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Stage).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Year).HasMaxLength(50);
        });

        // ==========================================
        // 3. Student Configuration
        // ==========================================
        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever(); // Ù„Ø£Ù† Ø§Ù„Ù€ ID Ø¨ÙŠØªÙ… Ø§Ù‚ØªØ±Ø§Ø­Ù‡ ÙˆØ¥Ø¯Ø®Ø§Ù„Ù‡ ÙŠØ¯ÙˆÙŠØ§Ù‹ ÙÙŠ Ø§Ù„ÙˆØ§Ø¬Ù‡Ø©
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Gender).HasMaxLength(20);
            entity.Property(e => e.GovGrade).HasMaxLength(50);
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)").HasDefaultValue(0);

            // Relation with ClassRoom
            entity.HasOne(e => e.ClassRoom)
                  .WithMany(c => c.Students)
                  .HasForeignKey(e => e.ClassRoomId)
                  .OnDelete(DeleteBehavior.Restrict); // Ù†Ù…Ù†Ø¹ Ù…Ø³Ø­ Ø§Ù„Ù ØµÙ„ Ù„Ùˆ Ø¬ÙˆØ§Ù‡ Ø·Ù„Ø§Ø¨

            // Relation with Family
            entity.HasOne(e => e.Family)
                  .WithMany(f => f.Siblings)
                  .HasForeignKey(e => e.FamilyId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // 3.5 Family Configuration
        // ==========================================
        modelBuilder.Entity<Family>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.FatherPhone).HasMaxLength(50);
            entity.Property(e => e.MotherPhone).HasMaxLength(50);
        });

        // ==========================================
        // 4. StudentGrade Configuration
        // ==========================================
        modelBuilder.Entity<StudentGrade>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Term).IsRequired().HasMaxLength(10);
            entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ExamScore).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.AttendanceScore).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            
            // Ø¹Ø´Ø§Ù† Ù†Ø¶Ù…Ù† Ø¥Ù† Ø§Ù„Ø·Ø§Ù„Ø¨ Ù…Ù„ÙˆØ´ ØºÙŠØ± Ø¯Ø±Ø¬Ø© ÙˆØ§Ø­Ø¯Ø© ÙÙŠ Ø§Ù„Ù…Ø§Ø¯Ø© Ù„Ù†ÙØ³ Ø§Ù„ØªÙŠØ±Ù…
            entity.HasIndex(e => new { e.StudentId, e.SubjectName, e.Term }).IsUnique();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.Grades)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 5. AttendanceRecord Configuration
        // ==========================================
        modelBuilder.Entity<AttendanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Term).HasMaxLength(10);
            entity.Property(e => e.AcademicYear).HasMaxLength(20);

            // Ù†Ù…Ù†Ø¹ ØªÙƒØ±Ø§Ø± Ø§Ù„ØºÙŠØ§Ø¨ Ù„Ù†ÙØ³ Ø§Ù„Ø·Ø§Ù„Ø¨ ÙÙŠ Ù†ÙØ³ Ø§Ù„ÙŠÙˆÙ…
            entity.HasIndex(e => new { e.StudentId, e.Date }).IsUnique();

            entity.HasOne(e => e.Student)
                  .WithMany(s => s.AttendanceRecords)
                  .HasForeignKey(e => e.StudentId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // 6. SubjectConfiguration & Fees
        // ==========================================
        modelBuilder.Entity<SubjectConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Stage).IsRequired().HasMaxLength(50);
            entity.Property(e => e.SubjectName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.MaxScoreTerm1).HasColumnType("decimal(5,2)").HasDefaultValue(0);
            entity.Property(e => e.MaxScoreTerm2).HasColumnType("decimal(5,2)").HasDefaultValue(0);
        });

        modelBuilder.Entity<StageFee>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StageName).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FeeAmount).HasColumnType("decimal(18,2)").HasDefaultValue(0);
        });

        // ==========================================
        // 7. StudentArchive Configuration
        // ==========================================
        modelBuilder.Entity<StudentArchive>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.AcademicYear, e.ClassName });
        });

        // ==========================================
        // 8. Tarbeya (Sunday School) Context
        // ==========================================
        modelBuilder.Entity<TarbeyaFamily>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<TarbeyaStage>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.Family).WithMany(f => f.Stages)
                  .HasForeignKey(e => e.FamilyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TarbeyaClass>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.Stage).WithMany(s => s.Classes)
                  .HasForeignKey(e => e.StageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TarbeyaArea>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<TarbeyaStudent>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(50);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ConfessionFather).HasMaxLength(150);
            entity.HasOne(e => e.Class).WithMany(c => c.Students)
                  .HasForeignKey(e => e.ClassId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AreaNavigation).WithMany(a => a.Students)
                  .HasForeignKey(e => e.AreaId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TarbeyaAttendance>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.HasOne(e => e.Student).WithMany(s => s.Attendances)
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.StudentId, e.Date }).IsUnique(); 
        });

        modelBuilder.Entity<TarbeyaLiturgyAttendance>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.HasOne(e => e.Student).WithMany()
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => new { e.StudentId, e.Date }).IsUnique(); 
        });

        modelBuilder.Entity<TarbeyaVisitationRecord>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Student).WithMany(s => s.Visitations)
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Servant).WithMany()
                  .HasForeignKey(e => e.ServantId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TarbeyaPointTransaction>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Student).WithMany(s => s.PointTransactions)
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Servant).WithMany()
                  .HasForeignKey(e => e.ServantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TarbeyaTrip>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.TicketPrice).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Family).WithMany()
                  .HasForeignKey(e => e.FamilyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TarbeyaTripSubscription>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AmountPaid).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Trip).WithMany(t => t.Subscriptions)
                  .HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Student).WithMany()
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Servant).WithMany()
                  .HasForeignKey(e => e.ServantId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TarbeyaTripExpense>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ItemDescription).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Trip).WithMany(t => t.Expenses)
                  .HasForeignKey(e => e.TripId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.AddedByFamilyAdmin).WithMany()
                  .HasForeignKey(e => e.AddedByFamilyAdminId).OnDelete(DeleteBehavior.Restrict);
        });

        // Family Finances
        modelBuilder.Entity<TarbeyaFamilyTransaction>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("decimal(18,2)");
            entity.Property(e => e.Category).HasMaxLength(150);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.HasOne(e => e.Family).WithMany()
                  .HasForeignKey(e => e.FamilyId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AddedByUser).WithMany()
                  .HasForeignKey(e => e.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
        });

        // Servants Management
        modelBuilder.Entity<ServantAttendance>(entity => {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Servant).WithMany()
                  .HasForeignKey(e => e.ServantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Family).WithMany()
                  .HasForeignKey(e => e.FamilyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ServiceTask>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.AssignedToServant).WithMany()
                  .HasForeignKey(e => e.AssignedToServantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.AssignedByAdmin).WithMany()
                  .HasForeignKey(e => e.AssignedByAdminId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Family).WithMany()
                  .HasForeignKey(e => e.FamilyId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(150);
            entity.HasOne(e => e.User).WithMany()
                  .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // Mahragan & Summer Clubs
        modelBuilder.Entity<MahraganEvent>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ThemeName).IsRequired().HasMaxLength(150);
        });

        modelBuilder.Entity<MahraganCompetition>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PassingScore).HasColumnType("decimal(18,2)");
            entity.HasOne(e => e.Event).WithMany(ev => ev.Competitions)
                  .HasForeignKey(e => e.EventId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.TargetStage).WithMany()
                  .HasForeignKey(e => e.TargetStageId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MahraganEnrollment>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BarcodeString).HasMaxLength(50);
            entity.HasOne(e => e.Student).WithMany()
                  .HasForeignKey(e => e.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.Competition).WithMany(c => c.Enrollments)
                  .HasForeignKey(e => e.CompetitionId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MahraganScore>(entity => {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).HasColumnType("decimal(18,2)");
            entity.Property(e => e.StageName).IsRequired().HasMaxLength(100);
            entity.HasOne(e => e.Enrollment).WithMany(en => en.Scores)
                  .HasForeignKey(e => e.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AppUser>()
            .HasOne(e => e.TarbeyaFamily)
            .WithMany()
            .HasForeignKey(e => e.TarbeyaFamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AppUser>()
            .HasOne(e => e.TarbeyaClass)
            .WithMany()
            .HasForeignKey(e => e.TarbeyaClassId)
            .OnDelete(DeleteBehavior.SetNull);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var auditEntries = new List<AuditLog>();
        string username = _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value 
            ?? _httpContextAccessor?.HttpContext?.User?.Identity?.Name 
            ?? "Unknown/System";

        var entries = ChangeTracker.Entries().Where(e => e.Entity is not AuditLog && (e.State == EntityState.Added || e.State == EntityState.Modified || e.State == EntityState.Deleted)).ToList();

        foreach (var entry in entries)
        {
            var entityName = entry.Entity.GetType().Name;
            var action = entry.State.ToString();
            
            var changes = new Dictionary<string, object>();
            if (entry.State == EntityState.Modified)
            {
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    var original = entry.OriginalValues[prop];
                    var current = entry.CurrentValues[prop];
                    if (!object.Equals(original, current))
                    {
                        changes[prop.Name] = new { Old = original, New = current };
                    }
                }
            }
            else if (entry.State == EntityState.Added)
            {
                foreach (var prop in entry.CurrentValues.Properties)
                {
                    changes[prop.Name] = entry.CurrentValues[prop];
                }
            }
            else if (entry.State == EntityState.Deleted)
            {
                foreach (var prop in entry.OriginalValues.Properties)
                {
                    changes[prop.Name] = entry.OriginalValues[prop];
                }
            }

            var audit = new AuditLog
            {
                Username = username,
                Action = action,
                EntityName = entityName,
                Timestamp = DateTime.UtcNow,
                Changes = JsonSerializer.Serialize(changes)
            };
            
            auditEntries.Add(audit);
        }

        if (auditEntries.Any())
        {
            AuditLogs.AddRange(auditEntries);
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

