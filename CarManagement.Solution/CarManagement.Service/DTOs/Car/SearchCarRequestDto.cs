namespace CarManagement.Service.DTOs.Car;

public sealed class SearchCarRequestDto
{
    /// <summary>
    /// Optional. The make of the car.
    /// </summary>
    public string? Make { get; set; }

    /// <summary>
    /// Optional. The model of the car.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// The page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The page size. The number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;
}
