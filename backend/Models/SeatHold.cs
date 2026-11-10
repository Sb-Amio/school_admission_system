namespace backend.Models;

public class SeatHold
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string ClassId { get; set; } = string.Empty;

    public SchoolClass Class { get; set; } = null!;

    public string StudentId { get; set; } = string.Empty;

    public string Status { get; set; } = "HELD";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime ExpiresAt { get; set; }
}
