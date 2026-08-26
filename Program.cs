using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SchoolSystemAPI.Data;
using SchoolSystemAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. إضافة الـ Controllers
builder.Services.AddControllers();

// 2. إعداد قاعدة البيانات (Entity Framework Core)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 3. تسجيل الـ UnitOfWork والـ Repositories
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// 4. تسجيل الـ Services اللي بنيناها
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IResultsService, ResultsService>();
builder.Services.AddScoped<IDocumentService, DocumentService>(); // صور الـ PNG والـ ZIP
builder.Services.AddScoped<IPdfService, PdfService>(); // الـ QuestPDF للشهادات
builder.Services.AddHttpContextAccessor();

// 5. إعداد الـ CORS عشان الواجهة (HTML/JS) تقدر تكلم الـ API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// 6. إعداد الـ Authentication والـ JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["SecretKey"] ?? "default_secret_key_needs_to_be_long_enough"))
    };
});

var app = builder.Build();

// إنشاء الأدمن الافتراضي عند أول تشغيل
using (var scope = app.Services.CreateScope())
{
    await DataSeeder.SeedAdminAsync(scope.ServiceProvider);
}

// تفعيل قراءة الملفات من مجلد wwwroot
app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = new List<string> { "Login.html" } // تحديد صفحة البداية الافتراضية
});
app.UseStaticFiles();

// 7. تفعيل الـ Middlewares
app.UseCors("AllowAll"); // تفعيل الـ CORS
app.UseAuthentication(); // تفعيل الحماية
app.UseAuthorization();  // تفعيل الصلاحيات

app.MapControllers();

app.Run();
