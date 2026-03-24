namespace CarManagement.Service.DTOs.Car;

public sealed class UpdateCarStockLevelRequestDto
{
    public Guid Id { get; set; }
    public int StockLevel { get; set; }
}
