namespace Webshop.Data;

public class PaymentVerificationAttempt
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int FailedAttempts { get; set; } = 0;

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}
