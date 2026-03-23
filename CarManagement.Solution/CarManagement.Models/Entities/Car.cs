namespace CarManagement.Models.Entities;

public class Car
{
    public Guid Id { get; init; }
    public Guid DealerId { get; init; }
    public string Make { get; init; } = null!;
    public string Model { get; init; } = null!;
    public int Year { get; init; }
    public string Colour { get; set; } = null!;
    public decimal Price { get; private set; }
    public int StockLevel { get; private set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Car(
        Guid dealerId, 
        string make, 
        string model, 
        int year, 
        string colour, 
        decimal price, 
        int stockLevel)
    {
        Id = Guid.NewGuid();
        DealerId = dealerId;
        Make = make;
        Model = model;
        Year = year;
        Colour = colour;

        if (price < 0) 
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");
        Price = price;

        if (stockLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(stockLevel), "Stock level cannot be negative.");
        StockLevel = stockLevel;

        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }
}
