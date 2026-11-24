using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Webshop.Data;

namespace Webshop.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class SetupModel : PageModel
{
    private const string UserKey = "SetupUserId";

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public SetupModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Temporary password")]
        public string TempPassword { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [Display(Name = "New password")]
        public string NewPassword { get; set; } = "";
    }

    public IActionResult OnGet()
    {
        if (TempData.Peek(UserKey) is null)
            return RedirectToPage("./Login");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (TempData.Peek(UserKey) is not string userId)
            return RedirectToPage("./Login");

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null || user.IsVerified)
        {
            TempData.Remove(UserKey);
            return RedirectToPage("./Login");
        }

        // Changes the password, which also saves the user row
        var change = await _userManager.ChangePasswordAsync(user, Input.TempPassword, Input.NewPassword);
        if (!change.Succeeded)
        {
            foreach (var error in change.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        // Mark as verified and save
        user.IsVerified = true;
        var update = await _userManager.UpdateAsync(user);
        if (!update.Succeeded)
        {
            foreach (var error in update.Errors)
                ModelState.AddModelError(string.Empty, error.Description);
            return Page();
        }

        TempData.Remove(UserKey);
        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToPage("./Manage/EnableAuthenticator");
    }
}