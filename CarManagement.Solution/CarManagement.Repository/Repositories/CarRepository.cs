using CarManagement.Common.Helpers;
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
                new { DealerId = dealerId.ToString(), Make = make, Model = model, Year = year, Colour = colour },
                cancellationToken: ct));

        return exists.HasValue;
    }

    /// <summary>
    /// Get car by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns the car if found, otherwise null.</returns>
    public async Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = "SELECT * FROM Cars WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        var row = await connection.QuerySingleOrDefaultAsync<CarRow>(
            new CommandDefinition(
                sql, 
                new { Id = id.ToString() }, 
                cancellationToken: ct));

        if (row is null)
        {
            return null;
        }

        return Car.Rehydrate(
            Guid.Parse(row.Id),
            Guid.Parse(row.DealerId),
            row.Make,
            row.Model,
            row.Year,
            row.Colour,
            row.Price,
            row.StockLevel,
            DateTimeOffset.Parse(row.CreatedAt),
            DateTimeOffset.Parse(row.UpdatedAt));
    }

    /// <summary>
    /// List cars by given dealer id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="Car"/>.</returns>
    public async Task<PagedResult<Car>> ListCarsAsync(Guid dealerId, int pageNumber, int pageSize, CancellationToken ct)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM Cars
            WHERE DealerId = @DealerId
            """;

        const string pageSql = """
            SELECT *
            FROM Cars
            WHERE DealerId = @DealerId
            ORDER BY Make, Model, Year, Colour
            LIMIT @PageSize
            OFFSET (@PageNumber - 1) * @PageSize
            """;

        using var connection = _connectionFactory.CreateConnection();

        var totalCount = await connection.QuerySingleAsync<int>(
            new CommandDefinition(countSql, new { DealerId = dealerId.ToString() }, cancellationToken: ct));

        var rows = await connection.QueryAsync<CarRow>(
            new CommandDefinition(
                pageSql, 
                new { DealerId = dealerId.ToString(), PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken: ct));

        return new PagedResult<Car>
        {
            Items = rows.Select(row => Car.Rehydrate(
                Guid.Parse(row.Id),
                Guid.Parse(row.DealerId),
                row.Make,
                row.Model,
                row.Year,
                row.Colour,
                row.Price,
                row.StockLevel,
                DateTimeOffset.Parse(row.CreatedAt),
                DateTimeOffset.Parse(row.UpdatedAt)
                )).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Remove car by id.
    /// </summary>
    /// <param name="id">The id of the car to remove.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car was removed, otherwise false.</returns>
    public async Task<bool> RemoveCarByIdAsync(Guid id, CancellationToken ct)
    {
        const string sql = "DELETE FROM Cars WHERE Id = @Id";

        using var connection = _connectionFactory.CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql, 
                new { Id = id.ToString() }, 
                cancellationToken: ct));

        return rowsAffected > 0;
    }

    /// <summary>
    /// Update car stock level by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="stockLevel">The new stock level.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the stock level was updated, otherwise false.</returns>
    public async Task<bool> UpdateCarStockLevelByIdAsync(Guid id, int stockLevel, CancellationToken ct)
    {
        const string sql = """
            UPDATE Cars
            SET StockLevel = @StockLevel, UpdatedAt = @UpdatedAt
            WHERE Id = @Id
            """;

        using var connection = _connectionFactory.CreateConnection();

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql, 
                new { Id = id.ToString(), StockLevel = stockLevel, UpdatedAt = DateTimeOffset.UtcNow.ToString("O") },
                cancellationToken: ct));

        return rowsAffected > 0;
    }

    /// <summary>
    /// Car row. Returns by the SQL query.
    /// </summary>
    private sealed class CarRow
    {
        public string Id { get; set; } = null!;
        public string DealerId { get; set; } = null!;
        public string Make { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public string Colour { get; set; } = null!;
        public decimal Price { get; set; }
        public int StockLevel { get; set; }
        public string CreatedAt { get; set; } = null!;
        public string UpdatedAt { get; set; } = null!;
    }
}
