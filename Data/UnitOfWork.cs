using SchoolSystemAPI.Models;

namespace SchoolSystemAPI.Data;

public interface IUnitOfWork : IDisposable
{
    IRepository<Student> Students { get; }
    IRepository<AttendanceRecord> AttendanceRecords { get; }
    IRepository<StudentGrade> StudentGrades { get; }
    IRepository<ClassRoom> ClassRooms { get; }
    IRepository<AppUser> Users { get; }
    IRepository<AppSetting> AppSettings { get; }
    IRepository<StageFee> StageFees { get; }
    IRepository<StudentArchive> StudentArchives { get; }
    IRepository<Excuse> Excuses { get; }
    IRepository<SubscriptionPayment> SubscriptionPayments { get; }
    IRepository<PendingRegistration> PendingRegistrations { get; }
    
    Task<int> CompleteAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public IRepository<Student> Students { get; private set; }
    public IRepository<AttendanceRecord> AttendanceRecords { get; private set; }
    public IRepository<StudentGrade> StudentGrades { get; private set; }
    public IRepository<ClassRoom> ClassRooms { get; private set; }
    public IRepository<AppUser> Users { get; private set; }
    public IRepository<AppSetting> AppSettings { get; private set; }
    public IRepository<StageFee> StageFees { get; private set; }
    public IRepository<StudentArchive> StudentArchives { get; private set; }
    public IRepository<Excuse> Excuses { get; private set; }
    public IRepository<SubscriptionPayment> SubscriptionPayments { get; private set; }
    public IRepository<PendingRegistration> PendingRegistrations { get; private set; }

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
        Students = new Repository<Student>(_context);
        AttendanceRecords = new Repository<AttendanceRecord>(_context);
        StudentGrades = new Repository<StudentGrade>(_context);
        ClassRooms = new Repository<ClassRoom>(_context);
        Users = new Repository<AppUser>(_context);
        AppSettings = new Repository<AppSetting>(_context);
        StageFees = new Repository<StageFee>(_context);
        StudentArchives = new Repository<StudentArchive>(_context);
        Excuses = new Repository<Excuse>(_context);
        SubscriptionPayments = new Repository<SubscriptionPayment>(_context);
        PendingRegistrations = new Repository<PendingRegistration>(_context);
    }

    public async Task<int> CompleteAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}


