namespace Webshop.Data;

public class Order
{
    public int Id { get; set; }

    public string OrderNumber { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public ApplicationUser User { get; set; } = null!;

    // Customer information
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;

    // Shipping information
    public string Country { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string StreetAddress { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? AdditionalInformation { get; set; }

    // Order total
    public long TotalPriceCents { get; set; }

    public string Currency { get; set; } = "EUR";

    public string OrderStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    // Related data
    public ICollection<OrderItem> Items { get; set; }
        = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; }
        = new List<Payment>();
}
