using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Webshop.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Webshop.Pages.Products;

[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public IList<Product> Products { get; set; } = new List<Product>();

    public async Task OnGetAsync()
    {
        Products = await _context.Products
            .Include(p => p.Stock)
            .AsNoTracking()
            .ToListAsync();
    }
    
public async Task<IActionResult> OnGetImageAsync(int id)
{
    var imageData = await _context.Products
        .Where(p => p.Id == id)
        .Select(p => p.ImageData)
        .FirstOrDefaultAsync();

    if (imageData == null || imageData.Length == 0)
        return NotFound();

    return File(imageData, "image/png");
}
}