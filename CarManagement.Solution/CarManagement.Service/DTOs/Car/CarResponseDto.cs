namespace CarManagement.Service.DTOs.Car;

public sealed class CarResponseDto
{
    /// <summary>
    /// The id of the car.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The id of the dealer who owns the car.
    /// </summary>
    public Guid DealerId { get; set; }

    /// <summary>
    /// The make of the car.
    /// </summary>
    public string Make { get; set; } = null!;

    /// <summary>
    /// The model of the car.
    /// </summary>
    public string Model { get; set; } = null!;

    /// <summary>
    /// The year of the car.
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// The colour of the car.
    /// </summary>
    public string Colour { get; set; } = null!;

    /// <summary>
    /// The price of the car.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// The stock level of the car.
    /// </summary>
    public int StockLevel { get; set; }

    /// <summary>
    /// The time the car was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The time the car was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; }
}
