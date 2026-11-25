using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Webshop.Data;

namespace Webshop.Areas.Identity.Pages.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        ILogger<IndexModel> logger)
    {
        _userManager = userManager;
        _logger = logger;
    }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required]
        [StringLength(100)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Phone]
        [StringLength(30)]
        [Display(Name = "Phone number")]
        public string? PhoneNumber { get; set; }

        [StringLength(100)]
        [Display(Name = "Country")]
        public string? Country { get; set; }

        [StringLength(100)]
        [Display(Name = "City")]
        public string? City { get; set; }

        [StringLength(255)]
        [Display(Name = "Street address")]
        public string? StreetAddress { get; set; }

        [StringLength(20)]
        [Display(Name = "Postal code")]
        public string? PostalCode { get; set; }

        [StringLength(500)]
        [Display(Name = "Additional information")]
        public string? AdditionalInformation { get; set; }
    }

    private async Task<ApplicationUser?> LoadUserAsync()
    {
        return await _userManager.GetUserAsync(User);
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await LoadUserAsync();

        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        Input = new InputModel
        {
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Country = user.Country,
            City = user.City,
            StreetAddress = user.StreetAddress,
            PostalCode = user.PostalCode,
            AdditionalInformation = user.AdditionalInformation
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var user = await LoadUserAsync();

        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        user.FirstName = Input.FirstName.Trim();
        user.LastName = Input.LastName.Trim();
        user.Country = Input.Country?.Trim();
        user.City = Input.City?.Trim();
        user.StreetAddress = Input.StreetAddress?.Trim();
        user.PostalCode = Input.PostalCode?.Trim();
        user.AdditionalInformation = Input.AdditionalInformation?.Trim();

        var phoneResult = await _userManager.SetPhoneNumberAsync(
            user,
            Input.PhoneNumber?.Trim());

        if (!phoneResult.Succeeded)
        {
            foreach (var error in phoneResult.Errors)
            {
                ModelState.AddModelError(
                    "Input.PhoneNumber",
                    error.Description);
            }

            return Page();
        }

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return Page();
        }

        _logger.LogInformation("User profile updated successfully.");

        StatusMessage = "Your profile has been updated successfully.";

        return RedirectToPage();
    }
}
