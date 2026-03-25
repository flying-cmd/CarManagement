namespace CarManagement.Service.DTOs.Car;

public sealed class RemoveCarRequestDto
{
    /// <summary>
    /// The id of the car to remove.
    /// </summary>
    public Guid Id { get; set; }
}
