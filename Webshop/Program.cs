using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using MySql.EntityFrameworkCore.Extensions;
using Webshop.Data;
using Microsoft.AspNetCore.Identity.UI.Services;
using Webshop.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySQL(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.Configure<IdentityOptions>(options =>
{
    options.Password.RequiredLength = 2;
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredUniqueChars = 1;
});
builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();
    builder.Services.AddTransient<IEmailSender, EmailSender>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleSeeder.AdminRole, policy => policy.RequireRole(RoleSeeder.AdminRole));
    options.AddPolicy(RoleSeeder.DbaRole, policy => policy.RequireRole(RoleSeeder.DbaRole));
});

// policy for /Dba
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOrDba", policy => policy.RequireRole("Admin", "DBA"));
});

builder.Services.AddRazorPages(options =>
{
    options.Conventions.AuthorizeFolder("/Admin", "AdminOrDba");
    options.Conventions.AuthorizeFolder("/Dba", "AdminOrDba");
});

// for temporary password
builder.Services.AddSingleton<Webshop.Services.EmailSender>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

    await emailSender.SendEmailAsync(
        "kkaappyyssttaaa@gmail.com",
        "WebShop SMTP Test",
        "<h1>SMTP works!</h1><p>Your email configuration is successful.</p>"
    );
}

using (var scope = app.Services.CreateScope())
{
    await RoleSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
