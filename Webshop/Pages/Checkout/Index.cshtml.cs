
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using Webshop.Data;

namespace Webshop.Pages.Checkout;

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

    public Webshop.Data.Cart? ShoppingCart { get; set; }

    public decimal TotalPrice { get; set; }

    [BindProperty]
    public CheckoutInputModel Input { get; set; } = new();

    public class CheckoutInputModel
    {
        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        [StringLength(30)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string StreetAddress { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [StringLength(500)]
        public string? AdditionalInformation { get; set; }
        
        public bool SaveToProfile { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
        {
            return Challenge();
        }

        await LoadCartAsync(user.Id);

        if (ShoppingCart == null || !ShoppingCart.Items.Any())
        {
            return RedirectToPage("/Cart/Index");
        }

        Input.FirstName = user.FirstName;
        Input.LastName = user.LastName;
        Input.Email = user.Email ?? string.Empty;
        Input.PhoneNumber = user.PhoneNumber ?? string.Empty;
        
        Input.Country = user.Country ?? string.Empty;
        Input.City = user.City ?? string.Empty;
        Input.StreetAddress = user.StreetAddress ?? string.Empty;
        Input.PostalCode = user.PostalCode ?? string.Empty;
        Input.AdditionalInformation = user.AdditionalInformation;

        return Page();
    }

    private async Task LoadCartAsync(string userId)
    {
        ShoppingCart = await _context.Carts
            .Include(c => c.Items)
            .ThenInclude(i => i.Product)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (ShoppingCart != null)
        {
            TotalPrice = ShoppingCart.Items.Sum(item =>
                item.Product.PriceCents * (decimal)item.Quantity / 100m);
        }
    }
    
    public async Task<IActionResult> OnPostAsync()
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
    {
        return Challenge();
    }

    await LoadCartAsync(user.Id);

    if (ShoppingCart == null || !ShoppingCart.Items.Any())
    {
        return RedirectToPage("/Cart/Index");
    }

    if (!ModelState.IsValid)
    {
        return Page();
    }

    // Calculate the total using database prices.
    long totalPriceCents = ShoppingCart.Items.Sum(item =>
        (long)item.Product.PriceCents * item.Quantity);

    var order = new Order
    {
        OrderNumber = $"ORD-{Guid.NewGuid().ToString("N")[..24].ToUpperInvariant()}",
        UserId = user.Id,

        FirstName = Input.FirstName.Trim(),
        LastName = Input.LastName.Trim(),
        Email = Input.Email.Trim(),
        PhoneNumber = Input.PhoneNumber.Trim(),

        Country = Input.Country.Trim(),
        City = Input.City.Trim(),
        StreetAddress = Input.StreetAddress.Trim(),
        PostalCode = Input.PostalCode.Trim(),
        AdditionalInformation = Input.AdditionalInformation?.Trim(),

        TotalPriceCents = totalPriceCents,
        Currency = "EUR",
        OrderStatus = "Pending",
        CreatedAt = DateTime.UtcNow
    };

    foreach (var item in ShoppingCart.Items)
    {
        var subtotal = (long)item.Product.PriceCents * item.Quantity;

        order.Items.Add(new OrderItem
        {
            ProductId = item.ProductId,
            ProductName = item.Product.Name,
            ProductModel = item.Product.Model,
            UnitPriceCents = item.Product.PriceCents,
            Quantity = item.Quantity,
            SubtotalCents = subtotal
        });
    }

    // Keep the user's preference for the payment step.
    TempData["SaveToProfile"] = Input.SaveToProfile;

    await using var transaction =
        await _context.Database.BeginTransactionAsync();

    try
    {
        _context.Orders.Add(order);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }

    return RedirectToPage("/Checkout/Payment",
        new { orderId = order.Id });
}
}
