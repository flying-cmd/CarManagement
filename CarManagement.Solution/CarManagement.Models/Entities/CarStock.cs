namespace CarManagement.Models.Entities;

/// <summary>
/// Represents dealer-specific car stock for a car, including stock level, unit price and last updated date.
/// </summary>
public class CarStock
{
    /// <summary>
    /// The id of the car stock.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The id of the dealer that owns the car stock.
    /// </summary>
    public Guid DealerId { get; private set; }

    /// <summary>
    /// The id of the car associated with this car stock record.
    /// </summary>
    public Guid CarId { get; private set; }

    /// <summary>
    /// The current stock level of the car.
    /// </summary>
    public int StockLevel { get; private set; }

    /// <summary>
    /// The unit price of the car.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// The last updated date of the car stock.
    /// </summary>
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CarStock"/> class.
    /// </summary>
    /// <param name="dealerId">The id of the dealer that owns the car stock.</param>
    /// <param name="carId">The id of the car associated with this car stock record.</param>
    /// <param name="stockLevel">The stock level of the car.</param>
    /// <param name="unitPrice">The unit price of the car.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when the dealerId or carId is empty,
    /// or when the stockLevel or unitPrice is less than zero.
    /// </exception>
    public CarStock(
        Guid dealerId,
        Guid carId, 
        int stockLevel, 
        decimal unitPrice)
    {
        Id = Guid.NewGuid();
        if (dealerId == Guid.Empty)
            throw new ArgumentException("Dealer id cannot be empty.", nameof(dealerId));
        DealerId = dealerId;

        if (carId == Guid.Empty)
            throw new ArgumentException("Car id cannot be empty.", nameof(carId));
        CarId = carId;

        if (stockLevel < 0)
            throw new ArgumentException("Stock level cannot be less than zero.", nameof(stockLevel));
        StockLevel = stockLevel;

        if (unitPrice < 0)
            throw new ArgumentException("Unit price cannot be less than zero.", nameof(unitPrice));
        UnitPrice = unitPrice;

        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the stock level of the car.
    /// </summary>
    /// <param name="stockLevel">The new stock level of the car.</param>
    /// <exception cref="ArgumentException">Thrown when the stock level is less than zero.</exception>
    public void UpdateStockLevel(int stockLevel)
    {
        if (stockLevel < 0)
            throw new ArgumentException("Stock level cannot be less than zero.", nameof(stockLevel));
        StockLevel = stockLevel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
