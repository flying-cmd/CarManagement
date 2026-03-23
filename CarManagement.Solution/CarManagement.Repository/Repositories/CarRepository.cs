using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using Dapper;

namespace CarManagement.Repository.Repositories;

public class CarRepository : ICarRepository
{
    private readonly SqliteConnectionFactory _connectionFactory;

    public CarRepository(SqliteConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    /// <summary>
    /// Add new car.
    /// </summary>
    /// <param name="car">The car entity to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns a task that represents the asynchronous operation.</returns>
    public async Task AddCarAsync(Car car, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO Cars (Id, DealerId, Make, Model, Year, Colour, Price, StockLevel, CreatedAt, UpdatedAt)
            VALUES (@Id, @DealerId, @Make, @Model, @Year, @Colour, @Price, @StockLevel, @CreatedAt, @UpdatedAt)
            """;

        var now = DateTimeOffset.UtcNow.ToString("O");

        using var connection = _connectionFactory.CreateConnection();

        await connection.ExecuteAsync(new CommandDefinition(
            sql, 
            new
            {
                Id = car.Id.ToString(),
                DealerId = car.DealerId.ToString(),
                Make = car.Make,
                Model = car.Model,
                Year = car.Year,
                Colour = car.Colour,
                Price = car.Price,
                StockLevel = car.StockLevel,
                CreatedAt = now,
                UpdatedAt = now
            },
            cancellationToken: ct));
    }

    /// <summary>
    /// Check if car exists given the dealer id, make, model, year and colour.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="colour">The colour of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car exists, otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid dealerId, string make, string model, int year, string colour, CancellationToken ct)
    {
        const string sql = """
            SELECT 1
            FROM Cars
            WHERE DealerId = @DealerId AND Make = @Make AND Model = @Model AND Year = @Year AND Colour = @Colour
            LIMIT 1
            """;

        using var connection = _connectionFactory.CreateConnection();

        var exists = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                sql, 
                new { DealerId = dealerId, Make = make, Model = model, Year = year, Colour = colour }, 
                cancellationToken: ct));

        return exists.HasValue;
    }
}
