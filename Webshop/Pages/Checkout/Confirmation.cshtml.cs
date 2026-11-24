using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;

namespace Webshop.Pages.Checkout;

[Authorize]
public class ConfirmationModel : PageModel
{
    private readonly ApplicationDbContext _context;
    
    private readonly UserManager<ApplicationUser> _userManager;
    
    public string? EmailWarning { get; set; }

    public ConfirmationModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Order? Order { get; set; }
    public Payment? Payment { get; set; }

    public async Task<IActionResult> OnGetAsync(int orderId)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        Order = await _context.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                o.UserId == user.Id);

        if (Order == null)
            return NotFound();

        if (Order.OrderStatus != "Paid")
            return BadRequest();

        Payment = await _context.Payments
            .AsNoTracking()
            .Where(p =>
                p.OrderId == Order.Id &&
                p.PaymentStatus == "Succeeded")
            .OrderByDescending(p => p.CreatedAt)
            .FirstOrDefaultAsync();

        if (Payment == null)
            return NotFound();
            
        EmailWarning = TempData["EmailWarning"] as string;

        return Page();
    }
}