namespace Webshop.Data;

public class Stock
{
    public int Id { get; set; }

    public int StockAmount { get; set; } = 0;

    public int ProductId { get; set; }

    public Product? Product { get; set; }
}