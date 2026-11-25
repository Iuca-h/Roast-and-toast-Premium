
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Webshop.Data;
using Webshop.Services;

namespace Webshop.Pages.Products;

public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly CartService _cartService;
    private readonly UserManager<ApplicationUser> _userManager;

    public DetailsModel(
        ApplicationDbContext context,
        CartService cartService,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _cartService = cartService;
        _userManager = userManager;
    }

    public Product? Product { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Product = await _context.Products
            .Include(p => p.Stock)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (Product == null)
        {
            return NotFound();
        }

        return Page();
    }

    [Authorize]
    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
        var userId = _userManager.GetUserId(User);

        if (userId == null)
        {
            return Challenge();
        }

        await _cartService.AddToCartAsync(userId, id);

        TempData["CartMessage"] =
            "Product successfully added to cart!";

        return RedirectToPage("/Products/Details", new { id });
    }
}
