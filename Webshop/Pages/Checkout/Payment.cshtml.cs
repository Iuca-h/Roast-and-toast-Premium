
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Net;
using System.Text;
using Webshop.Data;

namespace Webshop.Pages.Checkout;

[Authorize]
[EnableRateLimiting("PaymentVerification")]
public class PaymentModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly ILogger<PaymentModel> _logger;

    public PaymentModel(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        ILogger<PaymentModel> logger)
    {
        _context = context;
        _userManager = userManager;
        _emailSender = emailSender;
        _logger = logger;
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

    public string? PaymentError { get; set; }

    // GET: Display payment page.
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

        PaymentError = TempData["PaymentError"] as string;

        return Page();
    }

    // POST: Select payment method.
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

        if (PaymentMethod != "Card")
        {
            ModelState.AddModelError(
                nameof(PaymentMethod),
                "Please select a valid payment method.");

            Order = order;
            return Page();
        }

        return RedirectToPage(
            "/Checkout/Payment",
            new
            {
                orderId = order.Id,
                method = "Card"
            });
    }

    // POST: Validate test card details.
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

        return RedirectToPage(
            "/Checkout/Payment",
            new
            {
                orderId = order.Id,
                method = "Card",
                step = "3ds"
            });
    }

    // POST: Verify simulated 3D Secure code.
    public async Task<IActionResult> OnPostVerifyThreeDSecureAsync()
    {
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o =>
                o.Id == OrderId &&
                o.UserId == user.Id);

        if (order == null)
            return NotFound();

        // Prevent duplicate payment processing.
        if (order.OrderStatus == "Paid")
        {
            return RedirectToPage(
                "/Checkout/Confirmation",
                new { orderId = order.Id });
        }

        if (order.OrderStatus != "Pending")
            return BadRequest();

        var verifiedOrderId =
            TempData.Peek("CardVerifiedOrderId");

        if (verifiedOrderId == null ||
            !int.TryParse(
                Convert.ToString(verifiedOrderId),
                out var verifiedId) ||
            verifiedId != order.Id)
        {
            return BadRequest();
        }

        var now = DateTime.UtcNow;

        // LEVEL 2: Load user-based verification attempts.
        var userAttempt = await _context.UserPaymentVerificationAttempts
            .FirstOrDefaultAsync(a => a.UserId == user.Id);

        // Reset an expired user lockout or an expired counting window.
        if (userAttempt != null)
        {
            bool lockoutExpired =
                userAttempt.LockedUntil != null &&
                userAttempt.LockedUntil <= now;

            bool windowExpired =
                userAttempt.LockedUntil == null &&
                userAttempt.WindowStartedAt <= now.AddMinutes(-15);

            if (lockoutExpired || windowExpired)
            {
                userAttempt.FailedAttempts = 0;
                userAttempt.WindowStartedAt = now;
                userAttempt.LockedUntil = null;
                userAttempt.UpdatedAt = now;

                await _context.SaveChangesAsync();
            }
        }

        // Block all payment verification for this user.
        if (userAttempt?.LockedUntil > now)
        {
            ModelState.AddModelError(
                nameof(ThreeDSecureCode),
                "Too many incorrect attempts. Payment verification is temporarily locked for your account.");

            Order = order;
            PaymentMethod = "Card";
            ShowThreeDSecure = true;

            return Page();
        }

        // LEVEL 1: Load order-based verification attempts.
        var orderAttempt = await _context.PaymentVerificationAttempts
            .FirstOrDefaultAsync(a => a.OrderId == order.Id);

        // Reset an expired order lockout.
        if (orderAttempt?.LockedUntil != null &&
            orderAttempt.LockedUntil <= now)
        {
            orderAttempt.FailedAttempts = 0;
            orderAttempt.LockedUntil = null;
            orderAttempt.UpdatedAt = now;

            await _context.SaveChangesAsync();
        }

        // Block verification for this order.
        if (orderAttempt?.LockedUntil > now)
        {
            ModelState.AddModelError(
                nameof(ThreeDSecureCode),
                "Too many incorrect attempts. Please try again in 15 minutes.");

            Order = order;
            PaymentMethod = "Card";
            ShowThreeDSecure = true;

            return Page();
        }

        // Simulate a bank-declined payment.
        if (ThreeDSecureCode == "000000")
        {
            await using var failedTransaction =
                await _context.Database.BeginTransactionAsync();

            var failedAt = DateTime.UtcNow;

            var failedPayment = new Payment
            {
                OrderId = order.Id,
                TransactionId = $"TXN-{Guid.NewGuid():N}",
                PaymentMethod = "Card",
                PaymentStatus = "Failed",
                AmountCents = order.TotalPriceCents,
                Currency = order.Currency,
                CardBrand = "Visa",
                CardLast4 = "4242",
                CreatedAt = failedAt,
                ProcessedAt = failedAt
            };

            _context.Payments.Add(failedPayment);

            await _context.SaveChangesAsync();
            await failedTransaction.CommitAsync();

            TempData.Remove("CardVerifiedOrderId");

            TempData["PaymentError"] =
                "Your bank declined the payment. Please try again.";

            return RedirectToPage(
                "/Checkout/Payment",
                new
                {
                    orderId = order.Id,
                    method = "Card"
                });
        }

        // Handle an incorrect verification code.
        if (ThreeDSecureCode != "123456")
        {
            // Create order attempt record if it does not exist.
            if (orderAttempt == null)
            {
                orderAttempt = new PaymentVerificationAttempt
                {
                    OrderId = order.Id,
                    FailedAttempts = 0,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.PaymentVerificationAttempts.Add(orderAttempt);
            }

            // Increment order-based counter.
            orderAttempt.FailedAttempts++;
            orderAttempt.UpdatedAt = now;

            if (orderAttempt.FailedAttempts >= 5)
            {
                orderAttempt.LockedUntil = now.AddMinutes(15);
            }

            // Create user attempt record if it does not exist.
            if (userAttempt == null)
            {
                userAttempt = new UserPaymentVerificationAttempt
                {
                    UserId = user.Id,
                    FailedAttempts = 0,
                    WindowStartedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                _context.UserPaymentVerificationAttempts.Add(userAttempt);
            }

            // Increment user-based counter.
            userAttempt.FailedAttempts++;
            userAttempt.UpdatedAt = now;

            if (userAttempt.FailedAttempts >= 10)
            {
                userAttempt.LockedUntil = now.AddMinutes(15);
            }

            await _context.SaveChangesAsync();

            if (userAttempt.LockedUntil != null)
            {
                ModelState.AddModelError(
                    nameof(ThreeDSecureCode),
                    "Too many incorrect attempts. Payment verification is locked for your account for 15 minutes.");
            }
            else if (orderAttempt.LockedUntil != null)
            {
                ModelState.AddModelError(
                    nameof(ThreeDSecureCode),
                    "Too many incorrect attempts. This order is locked for 15 minutes.");
            }
            else
            {
                ModelState.AddModelError(
                    nameof(ThreeDSecureCode),
                    $"Invalid verification code. {5 - orderAttempt.FailedAttempts} attempts remaining for this order.");
            }

            Order = order;
            PaymentMethod = "Card";
            ShowThreeDSecure = true;

            return Page();
        }

        // Correct verification code.
        await using var transaction =
            await _context.Database.BeginTransactionAsync();

        // Check again before creating the payment.
        bool paymentAlreadyExists = await _context.Payments.AnyAsync(p =>
            p.OrderId == order.Id &&
            p.PaymentStatus == "Succeeded");

        if (paymentAlreadyExists)
        {
            await transaction.RollbackAsync();

            return RedirectToPage(
                "/Checkout/Confirmation",
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
                        error.Description);
                }

                Order = order;
                PaymentMethod = "Card";
                ShowThreeDSecure = true;

                return Page();
            }
        }

        var paymentTime = DateTime.UtcNow;

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
            CreatedAt = paymentTime,
            ProcessedAt = paymentTime
        };

        _context.Payments.Add(payment);

        order.OrderStatus = "Paid";
        order.UpdatedAt = paymentTime;

        // Remove order-based attempts after successful payment.
        if (orderAttempt != null)
        {
            _context.PaymentVerificationAttempts.Remove(orderAttempt);
        }

        // Keep the user-based counter until its 15-minute window expires.

        var cart = await _context.Carts
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.UserId == user.Id);

        if (cart != null)
        {
            _context.CartItems.RemoveRange(cart.Items);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        // Send confirmation email only after successful payment.
        try
        {
            var emailHtml = BuildOrderConfirmationEmail(order, payment);

            await _emailSender.SendEmailAsync(
                order.Email,
                $"Order confirmation - {order.OrderNumber}",
                emailHtml);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Payment succeeded, but confirmation email failed for order {OrderId}.",
                order.Id);

            TempData["EmailWarning"] =
                "Your payment was successful, but we couldn't send the confirmation email. You can view your order in My Orders.";
        }

        TempData.Remove("CardVerifiedOrderId");
        TempData.Remove("SaveToProfile");

        return RedirectToPage(
            "/Checkout/Confirmation",
            new { orderId = order.Id });
    }

    private string BuildOrderConfirmationEmail(Order order, Payment payment)
    {
        var culture = CultureInfo.GetCultureInfo("en-IE");

        string FormatPrice(long cents)
        {
            return (cents / 100m).ToString("C", culture);
        }

        string Safe(string? value)
        {
            return WebUtility.HtmlEncode(value ?? string.Empty);
        }

        var html = new StringBuilder();

        html.Append($"""
            <html>
            <body style="font-family: Arial, sans-serif; color: #333;">
                <h2>Thank you for your order, {Safe(order.FirstName)}!</h2>
                <p>Your payment was successful and your order has been confirmed.</p>

                <h3>Order details</h3>
                <p><strong>Order number:</strong> {Safe(order.OrderNumber)}</p>
                <p><strong>Order date:</strong> {order.CreatedAt:dd MMMM yyyy}</p>

                <h3>Items</h3>
                <table style="border-collapse: collapse; width: 100%;">
                    <tr>
                        <th style="text-align: left; padding: 8px; border-bottom: 1px solid #ddd;">Product</th>
                        <th style="padding: 8px; border-bottom: 1px solid #ddd;">Quantity</th>
                        <th style="text-align: right; padding: 8px; border-bottom: 1px solid #ddd;">Subtotal</th>
                    </tr>
            """);

        foreach (var item in order.Items)
        {
            html.Append($"""
                    <tr>
                        <td style="padding: 8px; border-bottom: 1px solid #ddd;">
                            {Safe(item.ProductName)} ({Safe(item.ProductModel)})
                        </td>
                        <td style="text-align: center; padding: 8px; border-bottom: 1px solid #ddd;">
                            {item.Quantity}
                        </td>
                        <td style="text-align: right; padding: 8px; border-bottom: 1px solid #ddd;">
                            {FormatPrice(item.SubtotalCents)}
                        </td>
                    </tr>
                """);
        }

        html.Append($"""
                </table>

                <h3>Total: {FormatPrice(order.TotalPriceCents)}</h3>

                <h3>Payment information</h3>
                <p><strong>Status:</strong> Successful</p>
                <p><strong>Method:</strong> Card</p>
                <p><strong>Card:</strong> {Safe(payment.CardBrand)} ending in {Safe(payment.CardLast4)}</p>
                <p><strong>Transaction ID:</strong> {Safe(payment.TransactionId)}</p>

                <p>Thank you for shopping with us!</p>
            </body>
            </html>
            """);

        return html.ToString();
    }
}
