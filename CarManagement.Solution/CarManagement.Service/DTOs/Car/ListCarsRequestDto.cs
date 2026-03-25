namespace CarManagement.Service.DTOs.Car;

public sealed class ListCarsRequestDto
{
    /// <summary>
    /// The page number.
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// The page size. The number of items per page.
    /// </summary>
    public int PageSize { get; set; } = 10;
}
