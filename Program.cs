
using keeptrack.Configuration;
using keeptrack.Data;
using keeptrack.Models;
using keeptrack.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =====================================================
// DATABASE — POSTGRESQL
// =====================================================

var databaseConnectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("LocalPostgresConnection")
    : builder.Configuration.GetConnectionString("PostgresConnection");

if (string.IsNullOrWhiteSpace(databaseConnectionString))
{
    throw new InvalidOperationException(
        "The PostgreSQL connection string for the current environment was not found."
    );
}

builder.Services.AddDbContext<PostgresKeepTrackDbContext>(options =>
    options.UseNpgsql(databaseConnectionString)
);

// =====================================================
// IDENTITY
// =====================================================

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;

    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<PostgresKeepTrackDbContext>()
.AddDefaultTokenProviders();

// =====================================================
// SESSION
// =====================================================

builder.Services.AddDistributedPostgresCache(options =>
{
    options.ConnectionString = databaseConnectionString;

    options.SchemaName = "public";
    options.TableName = "keeptrack_session_cache";
    options.CreateIfNotExists = true;
    options.UseWAL = false;
    options.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
    options.DefaultSlidingExpiration = TimeSpan.FromMinutes(30);
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.IdleTimeout = TimeSpan.FromMinutes(30);
});

// =====================================================
// RAZOR PAGES
// =====================================================

builder.Services.AddRazorPages();

// =====================================================
// EMAIL SETTINGS
// =====================================================

builder.Services.Configure<EmailSettings>(
    builder.Configuration.GetSection("EmailSettings"));

// =====================================================
// EMAIL SENDER
// =====================================================

builder.Services.AddTransient<IEmailSender, BrevoEmailSender>();

// =====================================================
// BACKGROUND SERVICES
// =====================================================

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddHostedService<TaskReminderBackgroundService>();
}

var app = builder.Build();

// =====================================================
// HTTP REQUEST PIPELINE
// =====================================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Session must be enabled before the page handlers run.
app.UseSession();

// =====================================================
// AUTHENTICATION & AUTHORIZATION
// =====================================================

app.UseAuthentication();
app.UseAuthorization();

// =====================================================
// RAZOR PAGES
// =====================================================

app.MapRazorPages();

app.Run();
