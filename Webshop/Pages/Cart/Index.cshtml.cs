
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Webshop.Data;
using Webshop.Services;

namespace Webshop.Pages.Cart;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly CartService _cartService;

    public IndexModel(
        ApplicationDbContext context,
        CartService cartService)
    {
        _context = context;
        _cartService = cartService;
    }

    public Webshop.Data.Cart? ShoppingCart { get; set; }

    public decimal TotalPrice { get; set; }

    public async Task OnGetAsync()
    {
        await LoadCartAsync();
    }

    private async Task LoadCartAsync()
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier);

        ShoppingCart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .ThenInclude(p => p.Stock)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (ShoppingCart != null)
        {
            TotalPrice = ShoppingCart.Items.Sum(item =>
                item.Product.PriceCents *
                (decimal)item.Quantity / 100m);
        }
    }

    public async Task<IActionResult> OnPostIncreaseAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == id && i.Cart.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        try
        {
            await _cartService.UpdateQuantityAsync(userId, id, item.Quantity + 1);
        }
        catch (InvalidOperationException ex)
        {
            TempData["CartError"] = ex.Message;
        }

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDecreaseAsync(int id)
    {
        var userId = User.FindFirstValue(
            ClaimTypes.NameIdentifier)!;

        var item = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == id && i.Cart.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        if (item.Quantity > 1)
        {
            await _cartService.UpdateQuantityAsync(
                userId, id, item.Quantity - 1);
        }
        else
        {
            await _cartService.RemoveFromCartAsync(
                userId, id);
        }

        return RedirectToPage();
    }
}
