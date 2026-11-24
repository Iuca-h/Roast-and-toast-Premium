using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Webshop.Data;

namespace Webshop.Pages.Cart;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Webshop.Data.Cart? ShoppingCart { get; set; }

    public decimal TotalPrice { get; set; }

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        ShoppingCart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (ShoppingCart != null)
        {
            TotalPrice = ShoppingCart.Items.Sum(item =>
                (decimal)item.Product.PriceCents * item.Quantity / 100m);
        }
    }
}