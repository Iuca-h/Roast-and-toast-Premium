
using Microsoft.EntityFrameworkCore;
using Webshop.Data;

namespace Webshop.Services;

public class CartService
{
    private readonly ApplicationDbContext _context;

    public CartService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Add a product to the cart
    public async Task AddToCartAsync(string userId, int productId)
    {
        var productExists = await _context.Products
            .AnyAsync(p => p.Id == productId);

        if (!productExists)
        {
            throw new InvalidOperationException("Product not found.");
        }

        var cart = await _context.Carts
            .FirstOrDefaultAsync(c => c.UserId == userId);

        if (cart == null)
        {
            cart = new Webshop.Data.Cart
            {
                UserId = userId
            };

            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }

        var cartItem = await _context.CartItems
            .FirstOrDefaultAsync(i =>
                i.CartId == cart.Id &&
                i.ProductId == productId);

        var stock = await GetAvailableStockAsync(productId);
        var currentQuantity = cartItem?.Quantity ?? 0;

        if (currentQuantity + 1 > stock)
        {
            throw new InvalidOperationException(
                stock == 0
                    ? "This product is out of stock."
                    : $"Only {stock} in stock. You already have {currentQuantity} in your cart.");
        }

        if (cartItem != null)
        {
            cartItem.Quantity++;
        }
        else
        {
            _context.CartItems.Add(new CartItem
            {
                CartId = cart.Id,
                ProductId = productId,
                Quantity = 1
            });
        }

        await _context.SaveChangesAsync();
    }

    // Update product quantity
    public async Task UpdateQuantityAsync(
        string userId,
        int cartItemId,
        int quantity)
    {
        if (quantity < 1)
        {
            throw new ArgumentException(
                "Quantity must be at least 1.");
        }

        var cartItem = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == cartItemId &&
                i.Cart.UserId == userId);

        if (cartItem == null)
        {
            throw new InvalidOperationException(
                "Cart item not found.");
        }

        var stock = await GetAvailableStockAsync(cartItem.ProductId);

        if (quantity > stock)
        {
            throw new InvalidOperationException(
                $"Only {stock} in stock.");
        }

        cartItem.Quantity = quantity;

        await _context.SaveChangesAsync();
    }

    // Remove a product from the cart
    public async Task RemoveFromCartAsync(
        string userId,
        int cartItemId)
    {
        var cartItem = await _context.CartItems
            .Include(i => i.Cart)
            .FirstOrDefaultAsync(i =>
                i.Id == cartItemId &&
                i.Cart.UserId == userId);

        if (cartItem == null)
        {
            throw new InvalidOperationException(
                "Cart item not found.");
        }

        _context.CartItems.Remove(cartItem);

        await _context.SaveChangesAsync();
    }

    private async Task<int> GetAvailableStockAsync(int productId)
    {
        // FirstOrDefaultAsync returns 0 when the product has no stock row
        return await _context.Stocks
            .Where(s => s.ProductId == productId)
            .Select(s => s.StockAmount)
            .FirstOrDefaultAsync();
    }
}
