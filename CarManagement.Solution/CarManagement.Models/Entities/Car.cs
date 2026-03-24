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

    /// <summary>
    /// Used for new car creation and rehydration.
    /// If no updated at is provided, it will be set to the created at.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="colour">The colour of the car.</param>
    /// <param name="price">The price of the car.</param>
    /// <param name="stockLevel">The stock level of the car.</param>
    /// <param name="createdAt">The creation date of the car.</param>
    /// <param name="updatedAt">The updated date of the car.</param>
    /// <exception cref="ArgumentException">Thrown when the id or dealer id is empty, make/model/colour is invalid or updatedAt is before createdAt.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the year, price or stock level is invalid.</exception>
    private Car(
        Guid id,
        Guid dealerId,
        string make,
        string model,
        int year,
        string colour,
        decimal price,
        int stockLevel,
        DateTimeOffset createdAt,
        DateTimeOffset? updatedAt = null)
    {
        var finalUpdatedAt = updatedAt ?? createdAt;

        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (dealerId == Guid.Empty)
            throw new ArgumentException("Dealer id cannot be empty.", nameof(dealerId));

        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("Make cannot be null, empty or whitespace.", nameof(make));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be null, empty or whitespace.", nameof(model));

        if (string.IsNullOrWhiteSpace(colour))
            throw new ArgumentException("Colour cannot be null, empty or whitespace.", nameof(colour));

        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "Year cannot be less than or equal to zero.");

        if (price < 0)
            throw new ArgumentOutOfRangeException(nameof(price), "Price cannot be negative.");

        if (stockLevel < 0)
            throw new ArgumentOutOfRangeException(nameof(stockLevel), "Stock level cannot be negative.");

        if (finalUpdatedAt < createdAt)
            throw new ArgumentException("Updated at cannot be before created at.", nameof(updatedAt));
    
        Id = id;
        DealerId = dealerId;
        Make = make;
        Model = model;
        Year = year;
        Colour = colour;
        Price = price;
        StockLevel = stockLevel;
        CreatedAt = createdAt;
        UpdatedAt = finalUpdatedAt;
    }

    /// <summary>
    /// Create a new car.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="colour">The colour of the car.</param>
    /// <param name="price">The price of the car.</param>
    /// <param name="stockLevel">The stock level of the car.</param>
    public Car(
        Guid dealerId, 
        string make, 
        string model, 
        int year, 
        string colour, 
        decimal price, 
        int stockLevel) : this(
            Guid.NewGuid(),
            dealerId,
            make,
            model,
            year,
            colour,
            price,
            stockLevel,
            DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Rehydrate a car. Rebuilds the existing car from the database.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="Make">The make of the car.</param>
    /// <param name="Model">The model of the car.</param>
    /// <param name="Year">The year of the car.</param>
    /// <param name="Colour">The colour of the car.</param>
    /// <param name="Price">The price of the car.</param>
    /// <param name="StockLevel">The stock level of the car.</param>
    /// <param name="createdAt">The creation date of the car.</param>
    /// <param name="updatedAt">The updated date of the car.</param>
    /// <returns></returns>
    public static Car Rehydrate(
        Guid id,
        Guid dealerId,
        string Make,
        string Model,
        int Year,
        string Colour,
        decimal Price,
        int StockLevel,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        return new Car(
            id,
            dealerId,
            Make,
            Model,
            Year,
            Colour,
            Price,
            StockLevel,
            createdAt,
            updatedAt);
    }
}
