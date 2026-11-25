using System.ComponentModel.DataAnnotations;
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

        public List<UserRow> Users { get; set; } = new();
        public List<ProductStockRow> Products { get; set; } = new();

        [BindProperty]
        public NewUserInput Input { get; set; } = new();

        public record UserRow(string Id, bool IsVerified, string Email, IList<string> Roles);
        public record ProductStockRow(int Id, string Name, int StockAmount);

        public class NewUserInput
        {
            [Required, EmailAddress]
            public string Email { get; set; } = "";
        }

        public async Task OnGetAsync()
        {
            await LoadUsersAsync();
            await LoadProductsAsync();
        }

        // Creates a User-role account with a generated password and emails it.
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadUsersAsync();
                await LoadProductsAsync();
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

                await LoadUsersAsync();
                await LoadProductsAsync();
                return Page();
            }

            var roleResult = await _userManager.AddToRoleAsync(user, RoleSeeder.UserRole);
            if (!roleResult.Succeeded)
            {
                foreach (var error in roleResult.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);

                await LoadUsersAsync();
                await LoadProductsAsync();
                return Page();
            }

            await _emailSender.SendEmailAsync(
                    Input.Email,
                    "WebShop SMTP Test",
                    $"<h1>Finish your webshop account!</h1><p>To login use this email and the temporary password: </p><strong>{tempPassword}</strong>"
                );

            var sent = true;

            TempData["Message"] = sent
                ? $"Created {Input.Email} and emailed a temporary password."
                : $"Created {Input.Email}. Email failed, so the temporary password is in the console.";

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

            // Stop people from deleting their own account while logged in
            if (user.Id == _userManager.GetUserId(User))
            {
                TempData["Message"] = "You can't delete your own account.";
                return RedirectToPage();
            }

            var result = await _userManager.DeleteAsync(user);
            TempData["Message"] = result.Succeeded
                ? $"Deleted {user.Email}."
                : "Could not delete: " + string.Join("; ", result.Errors.Select(e => e.Description));

            return RedirectToPage();
        }

        // Sets the stock for one product. Creates the stock row if it doesn't exist yet.
        public async Task<IActionResult> OnPostSetStockAsync(int productId, int stockAmount)
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
                var productExists = await _context.Products.AnyAsync(p => p.Id == productId);
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
            var users = _userManager.Users.OrderBy(u => u.Email).ToList();
            var rows = new List<UserRow>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                rows.Add(new UserRow(user.Id, user.IsVerified, user.Email ?? "", roles));
            }

            Users = rows;
        }

        private async Task LoadProductsAsync()
        {
            Products = await _context.Products
                .AsNoTracking()
                .Include(p => p.Stock)
                .OrderBy(p => p.Name)
                .Select(p => new ProductStockRow(p.Id, p.Name, p.Stock != null ? p.Stock.StockAmount : 0))
                .ToListAsync();
        }
    }
}