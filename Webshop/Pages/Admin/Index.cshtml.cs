using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;
using Webshop.Services;

namespace Webshop.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailSender _emailSender;
        private readonly ApplicationDbContext _context;

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            EmailSender emailSender,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _emailSender = emailSender;
            _context = context;
        }

        // Dashboard statistics
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public int TotalProducts { get; set; }
        public long TotalRevenueCents { get; set; }

        // Recent orders
        public List<OrderRow> RecentOrders { get; set; } = new();

        public record OrderRow(
            int Id,
            string OrderNumber,
            string CustomerName,
            string Email,
            DateTime CreatedAt,
            long TotalPriceCents,
            string OrderStatus
        );

        // Chart data
        public string[] OrderStatusLabels { get; set; } = Array.Empty<string>();
        public int[] OrderStatusValues { get; set; } = Array.Empty<int>();

        public string[] OrderDayLabels { get; set; } = Array.Empty<string>();
        public int[] OrderDayValues { get; set; } = Array.Empty<int>();

        // Existing management data
        public List<UserRow> Users { get; set; } = new();
        public List<ProductStockRow> Products { get; set; } = new();

        [BindProperty]
        public NewUserInput Input { get; set; } = new();

        public record UserRow(
            string Id,
            bool IsVerified,
            string Email,
            IList<string> Roles
        );

        public record ProductStockRow(
            int Id,
            string Name,
            int StockAmount
        );

        public class NewUserInput
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        private async Task LoadPageDataAsync()
        {
            await LoadDashboardAsync();
            await LoadUsersAsync();
            await LoadProductsAsync();
        }

        private async Task LoadDashboardAsync()
        {
            TotalUsers = await _userManager.Users.CountAsync();

            TotalOrders = await _context.Orders.CountAsync();

            TotalProducts = await _context.Products.CountAsync();

            TotalRevenueCents =
                await _context.Payments
                    .Where(p => p.PaymentStatus == "Succeeded")
                    .Select(p => (long?)p.AmountCents)
                    .SumAsync() ?? 0;

            // Order status distribution
            var statuses = await _context.Orders
                .AsNoTracking()
                .GroupBy(o => o.OrderStatus)
                .Select(g => new
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            OrderStatusLabels = statuses
                .Select(s => string.IsNullOrWhiteSpace(s.Status)
                    ? "Unknown"
                    : s.Status)
                .ToArray();

            OrderStatusValues = statuses
                .Select(s => s.Count)
                .ToArray();

            // Orders created during the last 7 days
            var today = DateTime.UtcNow.Date;
            var firstDay = today.AddDays(-6);
            var endDay = today.AddDays(1);

            var orderDates = await _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= firstDay &&
                            o.CreatedAt < endDay)
                .Select(o => o.CreatedAt)
                .ToListAsync();

            var dailyCounts = orderDates
                .GroupBy(date => date.Date)
                .ToDictionary(group => group.Key, group => group.Count());

            OrderDayLabels = Enumerable.Range(0, 7)
                .Select(i => firstDay.AddDays(i)
                    .ToString("dd MMM", CultureInfo.InvariantCulture))
                .ToArray();

            OrderDayValues = Enumerable.Range(0, 7)
                .Select(i =>
                {
                    var date = firstDay.AddDays(i);
                    return dailyCounts.TryGetValue(date, out var count)
                        ? count
                        : 0;
                })
                .ToArray();

            // Latest 5 orders
            RecentOrders = await _context.Orders
                .AsNoTracking()
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new OrderRow(
                    o.Id,
                    o.OrderNumber,
                    (o.FirstName ?? "") + " " + (o.LastName ?? ""),
                    o.Email,
                    o.CreatedAt,
                    o.TotalPriceCents,
                    o.OrderStatus
                ))
                .ToListAsync();
        }

        // Creates a User-role account with a generated password and emails it.
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync();
                return Page();
            }

            var tempPassword = PasswordHelper.Generate();

            var user = new ApplicationUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                EmailConfirmed = true
            };

            var createResult = await _userManager.CreateAsync(user, tempPassword);

            if (!createResult.Succeeded)
            {
                foreach (var error in createResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadPageDataAsync();
                return Page();
            }

            var roleResult = await _userManager.AddToRoleAsync(
                user,
                RoleSeeder.UserRole
            );

            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadPageDataAsync();
                return Page();
            }

            await _emailSender.SendEmailAsync(
                Input.Email,
                "WebShop SMTP Test",
                $"<h1>Finish your webshop account!</h1>" +
                $"<p>To login use this email and the temporary password:</p>" +
                $"<strong>{tempPassword}</strong>"
            );

            TempData["Message"] =
                $"Created {Input.Email} and emailed a temporary password.";

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);

            if (user is null)
            {
                TempData["Message"] = "User not found.";
                return RedirectToPage();
            }

            // Stop people from deleting their own account while logged in.
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Message"] = "You can't delete your own account.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);

            TempData["Message"] = result.Succeeded
                ? $"Deleted {user.Email}."
                : "Could not delete: " +
                  string.Join("; ", result.Errors.Select(e => e.Description));

            return RedirectToPage();
        }

        // Sets the stock for one product. Creates the stock row if it doesn't exist yet.
        public async Task<IActionResult> OnPostSetStockAsync(
            int productId,
            int stockAmount)
        {
            if (stockAmount < 0)
            {
                TempData["Message"] = "Stock can't be negative.";
                return RedirectToPage();
            }

            var stock = await _context.Stocks
                .FirstOrDefaultAsync(s => s.ProductId == productId);

            if (stock is null)
            {
                var productExists = await _context.Products
                    .AnyAsync(p => p.Id == productId);

                if (!productExists)
                {
                    TempData["Message"] = "Product not found.";
                    return RedirectToPage();
                }

                stock = new Stock { ProductId = productId };
                _context.Stocks.Add(stock);
            }

            stock.StockAmount = stockAmount;

            await _context.SaveChangesAsync();

            TempData["Message"] = "Stock updated.";

            return RedirectToPage();
        }

        private async Task LoadUsersAsync()
        {
            var users = await _userManager.Users
                .OrderBy(u => u.Email)
                .ToListAsync();

            var rows = new List<UserRow>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);

                rows.Add(new UserRow(
                    user.Id,
                    user.IsVerified,
                    user.Email ?? "",
                    roles
                ));
            }

            Users = rows;
        }

        private async Task LoadProductsAsync()
        {
            Products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Stock)
                .OrderBy(p => p.Name)
                .Select(p => new ProductStockRow(
                    p.Id,
                    p.Name,
                    p.Stock != null ? p.Stock.StockAmount : 0
                ))
                .ToListAsync();
        }
    }
}
