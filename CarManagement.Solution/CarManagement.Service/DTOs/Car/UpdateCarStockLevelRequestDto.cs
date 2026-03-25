namespace CarManagement.Service.DTOs.Car;

public sealed class UpdateCarStockLevelRequestDto
{
    /// <summary>
    /// The if of the car to update.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The new stock level of the car to update.
    /// </summary>
    public int StockLevel { get; set; }
}
