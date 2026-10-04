namespace Gemora.Domain.Entities;

public class EmailVerificationCode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }

    /*
     * Never store the real 6-digit OTP here.
     *
     * Example:
     * User receives: 482731
     * Database stores: hashed representation only.
     */
    public string CodeHash { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int AttemptCount { get; set; } = 0;

    /*
     * Null = code has not been successfully used.
     *
     * Has a value = verification completed using this code.
     */
    public DateTime? UsedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}