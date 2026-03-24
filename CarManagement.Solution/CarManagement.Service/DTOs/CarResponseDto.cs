namespace CarManagement.Service.DTOs;

public sealed class CarResponseDto
{
    public Guid Id { get; set; }
    public Guid DealerId { get; set; }
    public string Make { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string Colour { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
