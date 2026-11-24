using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;

namespace Webshop.Pages.MyOrders;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public IList<Order> Orders { get; set; } = new List<Order>();

    public async Task OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return;

        Orders = await _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == user.Id)
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }
}