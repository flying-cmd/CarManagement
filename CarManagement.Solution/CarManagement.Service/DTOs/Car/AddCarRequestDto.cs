namespace CarManagement.Service.DTOs.Car;

public sealed class AddCarRequestDto
{
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
}
