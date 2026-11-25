namespace Webshop.Data;

public class Product
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public uint PriceCents { get; set; }

    public string Capacity { get; set; } = string.Empty;

    public decimal PowerKw { get; set; }

    public string Material { get; set; } = string.Empty;

    public string Dimensions { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public byte[]? ImageData { get; set; }

    public string? ImageContentType { get; set; }

    public string? ImageFileName { get; set; }

    public Stock? Stock { get; set; }
}
