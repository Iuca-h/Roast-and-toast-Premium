using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Webshop.Data;

public static class RoleSeeder
{
    public const string UserRole = "User";
    public const string AdminRole = "Admin";
    public const string DbaRole = "DBA";

    public static async Task SeedAsync(IServiceProvider services, IConfiguration config)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        // Create the roles if they don't exist
        foreach (var role in new[] { UserRole, AdminRole, DbaRole })
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var roleResult = await roleManager.CreateAsync(new IdentityRole(role));
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            }
        }

        // Each user-secrets section maps to one role
        var seeds = new[]
        {
            (Section: "SeedAdmin", Role: AdminRole),
            (Section: "SeedDba",   Role: DbaRole),
            (Section: "SeedUser",  Role: UserRole),
        };

        foreach (var (section, role) in seeds)
        {
            var email = config[$"{section}:Email"];
            var password = config[$"{section}:Password"];

            // Skip this account if it isn't configured
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                continue;

            var user = await userManager.FindByEmailAsync(email);
            if (user is null)
            {
                user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create {section} user: " +
                        string.Join("; ", createResult.Errors.Select(e => e.Description)));
            }

            if (!await userManager.IsInRoleAsync(user, role))
            {
                var roleAssign = await userManager.AddToRoleAsync(user, role);
                if (!roleAssign.Succeeded)
                    throw new InvalidOperationException(
                        string.Join("; ", roleAssign.Errors.Select(e => e.Description)));
            }
        }
    }
}