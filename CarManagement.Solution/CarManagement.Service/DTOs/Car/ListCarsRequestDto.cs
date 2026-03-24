namespace CarManagement.Service.DTOs.Car;

public sealed class ListCarsRequestDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
