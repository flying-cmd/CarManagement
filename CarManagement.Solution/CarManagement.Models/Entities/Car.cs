namespace CarManagement.Models.Entities;

/// <summary>
/// Represents a car entity.
/// Stores basic car information that can be shared across multiple dealers.
/// </summary>
public class Car
{
    /// <summary>
    /// The id of the car.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// The make of the car.
    /// </summary>
    public string Make { get; private set; } = null!;

    /// <summary>
    /// The model of the car.
    /// </summary>
    public string Model { get; private set; } = null!;

    /// <summary>
    /// The year of the car.
    /// </summary>
    public int Year { get; private set; }

    /// <summary>
    /// The creation date of the car.
    /// </summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Used for new car creation and rehydration.
    /// If no updated at is provided, it will be set to the created at.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="createdAt">The creation date of the car.</param>
    /// <exception cref="ArgumentException">Thrown when the id or dealer id is empty, make/model/colour is invalid or updatedAt is before createdAt.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the year, price or stock level is invalid.</exception>
    private Car(
        Guid id,
        string make,
        string model,
        int year,
        DateTimeOffset createdAt)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id cannot be empty.", nameof(id));

        if (string.IsNullOrWhiteSpace(make))
            throw new ArgumentException("Make cannot be null, empty or whitespace.", nameof(make));

        if (string.IsNullOrWhiteSpace(model))
            throw new ArgumentException("Model cannot be null, empty or whitespace.", nameof(model));

        if (year <= 0)
            throw new ArgumentOutOfRangeException(nameof(year), "Year cannot be less than or equal to zero.");

        Id = id;
        Make = make;
        Model = model;
        Year = year;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Create a new car.
    /// </summary>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    public Car(
        string make, 
        string model, 
        int year) : this(
            Guid.NewGuid(),
            make,
            model,
            year,
            DateTimeOffset.UtcNow)
    {
    }

    /// <summary>
    /// Rehydrate a car. Rebuilds the existing car from the database.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="Make">The make of the car.</param>
    /// <param name="Model">The model of the car.</param>
    /// <param name="Year">The year of the car.</param>
    /// <param name="createdAt">The creation date of the car.</param>
    public static Car Rehydrate(
        Guid id,
        string Make,
        string Model,
        int Year,
        DateTimeOffset createdAt)
    {
        return new Car(
            id,
            Make,
            Model,
            Year,
            createdAt);
    }
}
