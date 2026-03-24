namespace CarManagement.Service.DTOs.Car;

public sealed class SearchCarRequestDto
{
    public string? Make { get; set; }
    public string? Model { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
