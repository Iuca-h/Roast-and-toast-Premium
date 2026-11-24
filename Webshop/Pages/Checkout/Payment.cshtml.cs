
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;

namespace Webshop.Pages.Checkout;

[Authorize]
public class PaymentModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public Order? Order { get; set; }

    [BindProperty]
    public int OrderId { get; set; }

    [BindProperty]
    public string PaymentMethod { get; set; } = "Card";

    [BindProperty]
    public string CardNumber { get; set; } = "4242424242424242";
    
    [BindProperty]
    public string ExpiryMonth { get; set; } = "12";
    
    [BindProperty]
    public string ExpiryYear { get; set; } = "2028";
    
    [BindProperty]
    public string Cvv { get; set; } = "123";

    public bool ShowThreeDSecure { get; set; }
    
    [BindProperty]
    public string ThreeDSecureCode { get; set; } = "123456";

    public async Task<IActionResult> OnGetAsync(
        int orderId,
        string? method,
        string? step)
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        Order = await _context.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.Id == orderId &&
                o.UserId == user.Id);

        if (Order == null)
            return NotFound();

        OrderId = Order.Id;

        PaymentMethod = method ?? "Card";
        
        if (PaymentMethod == "Card")
        {
            CardNumber = "4242424242424242";
            ExpiryMonth = "12";
            ExpiryYear = "2028";
            Cvv = "123";
        }

        if (step == "3ds")
        {
            var verifiedOrderId =
                TempData.Peek("CardVerifiedOrderId");

            if (verifiedOrderId == null ||
                Convert.ToInt32(verifiedOrderId) != orderId)
            {
                return BadRequest();
            }

            ShowThreeDSecure = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.Id == OrderId &&
                o.UserId == user.Id);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != "Pending")
            return BadRequest();

        var allowedMethods = new[]
        {
            "Card",
            "BankTransfer",
            "iDEAL"
        };

        if (!allowedMethods.Contains(PaymentMethod))
        {
            ModelState.AddModelError(
                nameof(PaymentMethod),
                "Please select a valid payment method.");

            Order = order;
            return Page();
        }

        return RedirectToPage("/Checkout/Payment",
            new
            {
                orderId = order.Id,
                method = PaymentMethod
            });
    }

    public async Task<IActionResult> OnPostProcessCardAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var order = await _context.Orders
            .FirstOrDefaultAsync(o =>
                o.Id == OrderId &&
                o.UserId == user.Id);

        if (order == null)
            return NotFound();

        if (order.OrderStatus != "Pending")
            return BadRequest();

        PaymentMethod = "Card";

        var normalizedCardNumber = CardNumber
            .Replace(" ", "")
            .Replace("-", "");

        var isTestCard =
            normalizedCardNumber == "4242424242424242";

        var isTestCvv = Cvv == "123";

        if (!isTestCard || !isTestCvv)
        {
            ModelState.AddModelError(
                string.Empty,
                "The test card details are invalid.");

            Order = order;
            return Page();
        }

        if (!int.TryParse(ExpiryMonth, out var month) ||
            !int.TryParse(ExpiryYear, out var year) ||
            month < 1 ||
            month > 12 ||
            year < 1)
        {
            ModelState.AddModelError(
                string.Empty,
                "Please enter a valid expiration date.");

            Order = order;
            return Page();
        }

        var currentDate = DateTime.UtcNow;

        if (year < currentDate.Year ||
            (year == currentDate.Year &&
             month < currentDate.Month))
        {
            ModelState.AddModelError(
                string.Empty,
                "The test card has expired.");

            Order = order;
            return Page();
        }

        // Store only the order ID, never card details.
        TempData["CardVerifiedOrderId"] = order.Id;

        return RedirectToPage("/Checkout/Payment",
            new
            {
                orderId = order.Id,
                method = "Card",
                step = "3ds"
            });
    }
    
    public async Task<IActionResult> OnPostVerifyThreeDSecureAsync()
{
    var user = await _userManager.GetUserAsync(User);

    if (user == null)
        return Challenge();

    var order = await _context.Orders.FirstOrDefaultAsync(o =>
        o.Id == OrderId && o.UserId == user.Id);

    if (order == null)
        return NotFound();

    // Prevent duplicate payment processing.
    if (order.OrderStatus == "Paid")
    {
        return RedirectToPage("/Checkout/Confirmation",
            new { orderId = order.Id });
    }

    if (order.OrderStatus != "Pending")
        return BadRequest();

    var verifiedOrderId = TempData.Peek("CardVerifiedOrderId");

    if (verifiedOrderId == null ||
        !int.TryParse(Convert.ToString(verifiedOrderId), out var verifiedId) ||
        verifiedId != order.Id)
    {
        return BadRequest();
    }

    if (ThreeDSecureCode != "123456")
    {
        ModelState.AddModelError(
            nameof(ThreeDSecureCode),
            "Invalid verification code."
        );

        Order = order;
        PaymentMethod = "Card";
        ShowThreeDSecure = true;

        return Page();
    }

    await using var transaction =
        await _context.Database.BeginTransactionAsync();

    // Check again before creating the payment.
    bool paymentAlreadyExists = await _context.Payments.AnyAsync(p =>
        p.OrderId == order.Id &&
        p.PaymentStatus == "Succeeded");

    if (paymentAlreadyExists)
    {
        await transaction.RollbackAsync();

        return RedirectToPage("/Checkout/Confirmation",
            new { orderId = order.Id });
    }

    // Save shipping details to the user's profile only if requested.
    bool saveToProfile =
        TempData["SaveToProfile"] is bool save && save;

    if (saveToProfile)
    {
        user.FirstName = order.FirstName;
        user.LastName = order.LastName;
        user.PhoneNumber = order.PhoneNumber;
        user.Country = order.Country;
        user.City = order.City;
        user.StreetAddress = order.StreetAddress;
        user.PostalCode = order.PostalCode;
        user.AdditionalInformation = order.AdditionalInformation;

        var updateResult = await _userManager.UpdateAsync(user);

        if (!updateResult.Succeeded)
        {
            await transaction.RollbackAsync();

            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    error.Description
                );
            }

            Order = order;
            PaymentMethod = "Card";
            ShowThreeDSecure = true;

            return Page();
        }
    }

    var now = DateTime.UtcNow;

    var payment = new Payment
    {
        OrderId = order.Id,
        TransactionId = $"TXN-{Guid.NewGuid():N}",
        PaymentMethod = "Card",
        PaymentStatus = "Succeeded",
        AmountCents = order.TotalPriceCents,
        Currency = order.Currency,
        CardBrand = "Visa",
        CardLast4 = "4242",
        CreatedAt = now,
        ProcessedAt = now
    };

    _context.Payments.Add(payment);

    order.OrderStatus = "Paid";
    order.UpdatedAt = now;

    // Clear the user's shopping cart.
    var cart = await _context.Carts
        .Include(c => c.Items)
        .FirstOrDefaultAsync(c => c.UserId == user.Id);

    if (cart != null)
    {
        _context.CartItems.RemoveRange(cart.Items);
    }

    await _context.SaveChangesAsync();
    await transaction.CommitAsync();

    TempData.Remove("CardVerifiedOrderId");
    TempData.Remove("SaveToProfile");

    return RedirectToPage("/Checkout/Confirmation",
        new { orderId = order.Id });
}
}
