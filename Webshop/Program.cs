using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;
using Webshop.Services;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Session
builder.Services.AddDistributedMemoryCache();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.IsEssential = true;
});

// Identity password settings
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 6;

    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
});

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;

    options.Lockout.MaxFailedAccessAttempts = 3;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.AllowedForNewUsers = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddScoped<
    IPasswordValidator<ApplicationUser>,
    StrongPasswordValidator>();

// Email
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSingleton<EmailSender>();

// Cart
builder.Services.AddScoped<CartService>();

// Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("PaymentVerification", httpContext =>
    {
        // Do not rate-limit GET requests.
        if (!HttpMethods.IsPost(httpContext.Request.Method))
        {
            return RateLimitPartition.GetNoLimiter<string>("payment-get");
        }

        var userId = httpContext.User.FindFirst(
            System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        var partitionKey = $"{userId ?? "anonymous"}:{ipAddress}";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});


// Authorization
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleSeeder.AdminRole, policy =>
        policy.RequireRole(RoleSeeder.AdminRole));

    options.AddPolicy(RoleSeeder.DbaRole, policy =>
        policy.RequireRole(RoleSeeder.DbaRole));

    options.AddPolicy("AdminOrDba", policy =>
        policy.RequireRole("Admin", "DBA"));
});

// Razor Pages
builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOrDba");
    options.Conventions.AuthorizeFolder("/Dba", "DBA");
});

var app = builder.Build();

// Seed roles, users and Products
using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
    
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await ProductSeeder.SeedAsync(db);
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// HTTP Security Headers
app.Use(async (context, next) =>
{
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";

    context.Response.Headers["Referrer-Policy"] =
        "strict-origin-when-cross-origin";

    context.Response.Headers["Permissions-Policy"] =
        "camera=(), microphone=(), geolocation=()";

    context.Response.Headers["X-Frame-Options"] = "DENY";

    context.Response.Headers["Content-Security-Policy"] =
    "default-src 'self'; " +
    "script-src 'self' 'unsafe-inline'; " +
    "style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; " +
    "img-src 'self' data: blob:; " +
    "font-src 'self' data: https://fonts.gstatic.com; " +
    "connect-src 'self'; " +
    "object-src 'none'; " +
    "base-uri 'self'; " +
    "form-action 'self'; " +
    "frame-ancestors 'none'";

    await next();
});

app.UseRouting();
app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
