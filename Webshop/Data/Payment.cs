namespace Webshop.Data;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public string TransactionId { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = string.Empty;

    public string PaymentStatus { get; set; } = "Pending";

    public long AmountCents { get; set; }

    public string Currency { get; set; } = "EUR";

    // Safe card metadata only
    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public string? PaymentReference { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }
}
