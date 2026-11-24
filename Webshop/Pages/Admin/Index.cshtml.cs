using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Webshop.Data;

namespace Webshop.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;

        public IndexModel(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        public List<UserRow> Users { get; set; } = new();

        [BindProperty]
        public NewUserInput Input { get; set; } = new();

        public record UserRow(string Id, string Email, IList<string> Roles);

        public class NewUserInput
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";

            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            await LoadUsersAsync();
        }

        // Creates a user. The role is hardcoded to User, so the form can't choose another.
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUsersAsync();
                return Page();
            }

            var user = new IdentityUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, Input.Password);
            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadUsersAsync();
                return Page();
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RoleSeeder.UserRole);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadUsersAsync();
                return Page();
            }

            TempData["Message"] = $"Created {Input.Email} with role User.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user is null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToPage();
            }

            // Stop people from deleting their own account while logged in
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Message"] = "You can't delete your own account.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            TempData["Message"] = result.Succeeded
                ? $"Deleted {user.Email}."
                : "Could not delete: " + string.Join("; ", result.Errors.Select(e => e.Description));

            return RedirectToPage();
        }

        private async Task LoadUsersAsync()
        {
            var users = _userManager.Users.OrderBy(u => u.Email).ToList();
            var rows = new List<UserRow>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserRow(user.Id, user.Email ?? "", roles));
            }

            Users = rows;
        }
    }
}