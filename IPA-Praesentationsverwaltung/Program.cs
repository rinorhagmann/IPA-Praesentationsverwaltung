using IPA_Praesentationsverwaltung.Data;
using IPA_Praesentationsverwaltung.Infrastructure;
using IPA_Praesentationsverwaltung.Services;
using IPA_Praesentationsverwaltung.Services.Abstractions;
using IPA_Praesentationsverwaltung.Services.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// MVC
// ---------------------------------------------------------------------------
builder.Services.AddControllersWithViews();

// ---------------------------------------------------------------------------
// Data access (MSSQL via Entity Framework Core)
// ---------------------------------------------------------------------------
string connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()));

// ---------------------------------------------------------------------------
// Application services (registered against their abstractions for loose coupling)
// ---------------------------------------------------------------------------
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
builder.Services.AddSingleton<ILoginThrottleService, MemoryLoginThrottleService>();
builder.Services.AddSingleton<IEmailSender, FileEmailSender>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAssignmentRuleService, AssignmentRuleService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<IPresentationService, PresentationService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<ICsvImportService, CsvImportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<ISystemResetService, SystemResetService>();
builder.Services.AddScoped<IAdminAccountService, AdminAccountService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// ---------------------------------------------------------------------------
// Authentication & authorization (cookie based, role aware)
// ---------------------------------------------------------------------------
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        // Require HTTPS for the auth cookie whenever the request is secure.
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP request pipeline
// ---------------------------------------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Apply migrations and seed the default administrator on startup.
await DbInitializer.InitializeAsync(app.Services);

app.Run();

// Exposed so the integration/unit test host can reference the entry-point assembly.
public partial class Program { }
