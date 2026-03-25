namespace CarManagement.Models.Entities;

public class CarStock
{
    public Guid Id { get; init; }
    public Guid DealerId { get; private set; }
    public Guid CarId { get; private set; }
    public int StockLevel { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

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

    public void UpdateStockLevel(int stockLevel)
    {
        if (stockLevel < 0)
            throw new ArgumentException("Stock level cannot be less than zero.", nameof(stockLevel));
        StockLevel = stockLevel;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
