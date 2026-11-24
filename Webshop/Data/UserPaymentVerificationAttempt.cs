namespace Webshop.Data;

public class UserPaymentVerificationAttempt
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;

    public int FailedAttempts { get; set; } = 0;

    public DateTime WindowStartedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ApplicationUser User { get; set; } = null!;
}