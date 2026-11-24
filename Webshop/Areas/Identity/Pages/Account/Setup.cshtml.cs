
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

    public SetupModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
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
        if (HttpContext.Session.GetString(UserKey) is null)
            return RedirectToPage("./Login");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Console.WriteLine("SETUP POST STARTED");

        var userId = HttpContext.Session.GetString(UserKey);

        Console.WriteLine($"User ID exists: {userId != null}");

        if (userId is null)
            return RedirectToPage("./Login");

        if (!ModelState.IsValid)
            return Page();

        var user = await _userManager.FindByIdAsync(userId);

        if (user is null || user.IsVerified)
        {
            HttpContext.Session.Remove(UserKey);
            return RedirectToPage("./Login");
        }

        var result = await _userManager.ChangePasswordAsync(
            user,
            Input.TempPassword,
            Input.NewPassword
        );

        Console.WriteLine($"Change password succeeded: {result.Succeeded}");

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"{error.Code}: {error.Description}");
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }

        user.IsVerified = true;

        var updateResult = await _userManager.UpdateAsync(user);

        Console.WriteLine($"Update user succeeded: {updateResult.Succeeded}");

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                Console.WriteLine($"{error.Code}: {error.Description}");
                ModelState.AddModelError("", error.Description);
            }

            return Page();
        }

        var checkUser = await _userManager.FindByIdAsync(user.Id);

        Console.WriteLine($"Database IsVerified: {checkUser?.IsVerified}");

        HttpContext.Session.Remove(UserKey);

        await _signInManager.SignInAsync(user, isPersistent: false);

        return RedirectToPage("./Manage/EnableAuthenticator");
    }
}
