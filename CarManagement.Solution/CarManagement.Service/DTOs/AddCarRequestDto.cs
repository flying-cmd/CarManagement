namespace CarManagement.Service.DTOs;

public class AddCarRequestDto
{
    public string Make { get; set; } = null!;
    public string Model { get; set; } = null!;
    public int Year { get; set; }
    public string Colour { get; set; } = null!;
    public decimal Price { get; set; }
    public int StockLevel { get; set; }
}
