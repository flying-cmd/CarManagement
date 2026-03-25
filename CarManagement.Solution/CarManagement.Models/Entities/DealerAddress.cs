namespace CarManagement.Models.Entities;

public class DealerAddress
{
    public Guid Id { get; init; }
    public Guid DealerId { get; private set; }
    public string Line { get; private set; } = null!;
    public string Suburb { get; private set; } = null!;
    public string State { get; private set; } = null!;
    public string Postcode { get; private set; } = null!;
    public string Country { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; init; }
}
