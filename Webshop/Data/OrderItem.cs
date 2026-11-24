namespace Webshop.Data;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int? ProductId { get; set; }

    public Product? Product { get; set; }

    // Product snapshot at purchase time
    public string ProductName { get; set; } = string.Empty;

    public string ProductModel { get; set; } = string.Empty;

    public long UnitPriceCents { get; set; }

    public int Quantity { get; set; }

    public long SubtotalCents { get; set; }
}
