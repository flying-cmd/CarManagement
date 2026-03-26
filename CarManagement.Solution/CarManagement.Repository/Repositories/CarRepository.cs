using CarManagement.Common.Helpers;
using CarManagement.DataAccess.Data;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using Dapper;
using System.Data;

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
    public async Task<bool> AddCarAsync(Car car, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO Cars (Id, Make, Model, Year, CreatedAt)
            VALUES (@Id, @Make, @Model, @Year, @CreatedAt);
            """;

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = car.Id.ToString(),
                        car.Make,
                        car.Model,
                        car.Year,
                        CreatedAt = car.CreatedAt.ToString("O")
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            return rowsAffected > 0;
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Check if car exists given the dealer id, make, model and year.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="colour">The colour of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car exists, otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid dealerId, string make, string model, int year, CancellationToken ct)
    {
        const string sql = """
            SELECT 1
            FROM CarStocks cs
            INNER JOIN Cars c ON c.Id = cs.CarId
            WHERE cs.DealerId = @DealerId AND c.Make = @Make AND c.Model = @Model AND c.Year = @Year
            LIMIT 1
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var exists = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(
                sql,
                new { DealerId = dealerId.ToString(), Make = make, Model = model, Year = year },
                cancellationToken: ct));

        return exists.HasValue;
    }

    /// <summary>
    /// Get car by make, model and year.
    /// </summary>
    /// <param name="make">The make of the car.</param>
    /// <param name="model">The model of the car.</param>
    /// <param name="year">The year of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">The DB connection.</param>
    /// <param name="transaction">The DB transaction.</param>
    /// <returns>Returns <see cref="Car"/> if found, otherwise null.</returns>
    public async Task<Car?> GetByMakeModelYearAsync(string make, string model, int year, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = """
            SELECT *
            FROM Cars
            WHERE Make = @Make AND Model = @Model AND Year = @Year
            LIMIT 1
            """;

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<CarRow>(
                new CommandDefinition(
                    sql,
                    new { Make = make, Model = model, Year = year },
                    transaction: transaction,
                    cancellationToken: ct));

            if (row is null)
            {
                return null;
            }

            return Car.Rehydrate(
                Guid.Parse(row.Id),
                row.Make,
                row.Model,
                row.Year,
                DateTimeOffset.Parse(row.CreatedAt));
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Get car by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="Car"/> if found, otherwise null.</returns>
    public async Task<Car?> GetCarByIdAsync(Guid id, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT * FROM Cars WHERE Id = @Id";

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var row = await connection.QuerySingleOrDefaultAsync<CarRow>(
            new CommandDefinition(
                sql,
                new { Id = id.ToString() },
                transaction: transaction,
                cancellationToken: ct));

            if (row is null)
            {
                return null;
            }

            return Car.Rehydrate(
                Guid.Parse(row.Id),
                row.Make,
                row.Model,
                row.Year,
                DateTimeOffset.Parse(row.CreatedAt));
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// List cars by given dealer id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="Car"/>.</returns>
    public async Task<PagedResult<CarWithStockRow>> ListCarsAsync(Guid dealerId, int pageNumber, int pageSize, CancellationToken ct)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM CarStocks cs
            INNER JOIN Cars c ON c.Id = cs.CarId
            WHERE cs.DealerId = @DealerId;
            """;

        const string pageSql = """
            SELECT
                c.Id,
                cs.DealerId,
                cs.Id AS CarStockId,
                c.Make,
                c.Model,
                c.Year,
                c.CreatedAt,
                cs.StockLevel,
                cs.UnitPrice,
                cs.UpdatedAt AS StockUpdatedAt
            FROM CarStocks cs
            INNER JOIN Cars c ON c.Id = cs.CarId
            WHERE cs.DealerId = @DealerId
            ORDER BY c.Make, c.Model, c.Year
            LIMIT @PageSize
            OFFSET (@PageNumber - 1) * @PageSize;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var totalCount = await connection.QuerySingleAsync<int>(
            new CommandDefinition(countSql, new { DealerId = dealerId.ToString() }, cancellationToken: ct));

        var rows = await connection.QueryAsync<CarWithStockRow>(
            new CommandDefinition(
                pageSql,
                new { DealerId = dealerId.ToString(), PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken: ct));

        return new PagedResult<CarWithStockRow>
        {
            Items = rows.ToList(),
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
    public async Task<bool> RemoveCarByIdAsync(Guid id, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = "DELETE FROM Cars WHERE Id = @Id";

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);
        
        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new { Id = id.ToString() },
                    transaction: transaction,
                    cancellationToken: ct));

            return rowsAffected > 0;
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Search cars owned by the given dealer with optional make and model filters.
    /// </summary>
    /// <param name="dealerId">The id of the dealer whose cars to search.</param>
    /// <param name="make">Optional. The make of the car. When provided, cars whose make contains this string will be returned.</param>
    /// <param name="model">Optional. The model of the car. When provided, cars whose model contains this string will be returned.</param>
    /// <param name="pageNumber">The page number.</param>
    /// <param name="pageSize">The page size. The number of items to return per page.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns <see cref="PagedResult{T}"/> where T is <see cref="Car"/>.</returns>
    public async Task<PagedResult<CarWithStockRow>> SearchCarsAsync(Guid dealerId, string? make, string? model, int pageNumber, int pageSize, CancellationToken ct)
    {
        const string countSql = """
            SELECT COUNT(*)
            FROM CarStocks cs
            INNER JOIN Cars c ON c.Id = cs.CarId
            WHERE cs.DealerId = @DealerId
            AND (@Make IS NULL OR LOWER(c.Make) LIKE '%' || LOWER(@Make) || '%')
            AND (@Model IS NULL OR LOWER(c.Model) LIKE '%' || LOWER(@Model) || '%')
            """;

        const string pageSql = """
            SELECT
                c.Id,
                cs.DealerId,
                cs.Id AS CarStockId,
                c.Make,
                c.Model,
                c.Year,
                c.CreatedAt,
                cs.StockLevel,
                cs.UnitPrice,
                cs.UpdatedAt AS StockUpdatedAt
            FROM CarStocks cs
            INNER JOIN Cars c ON c.Id = cs.CarId
            WHERE cs.DealerId = @DealerId
            AND (@Make IS NULL OR LOWER(c.Make) LIKE '%' || LOWER(@Make) || '%')
            AND (@Model IS NULL OR LOWER(c.Model) LIKE '%' || LOWER(@Model) || '%')
            ORDER BY c.Make, c.Model, c.Year
            LIMIT @PageSize
            OFFSET (@PageNumber - 1) * @PageSize;
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var totalCount = await connection.QuerySingleAsync<int>(
            new CommandDefinition(
                countSql,
                new { DealerId = dealerId.ToString(), Make = make, Model = model },
                cancellationToken: ct));

        var rows = await connection.QueryAsync<CarWithStockRow>(
            new CommandDefinition(
                pageSql,
                new { DealerId = dealerId.ToString(), Make = make, Model = model, PageNumber = pageNumber, PageSize = pageSize },
                cancellationToken: ct));

        return new PagedResult<CarWithStockRow>
        {
            Items = rows.ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    /// <summary>
    /// Update car stock level by id.
    /// </summary>
    /// <param name="id">The id of the car.</param>
    /// <param name="stockLevel">The new stock level.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the stock level was updated, otherwise false.</returns>
    public async Task<bool> UpdateCarStockLevelAsync(Guid carId, Guid dealerId, int stockLevel, CancellationToken ct)
    {
        const string sql = """
            UPDATE CarStocks
            SET StockLevel = @StockLevel, UpdatedAt = @UpdatedAt
            WHERE CarId = @CarId AND DealerId = @DealerId
            """;

        using var connection = await _connectionFactory.CreateConnectionAsync(ct);

        var rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(
                sql,
                new 
                { 
                    CarId = carId.ToString(), 
                    DealerId = dealerId.ToString(), 
                    StockLevel = stockLevel, 
                    UpdatedAt = DateTimeOffset.UtcNow.ToString("O") 
                },
                cancellationToken: ct));

        return rowsAffected > 0;
    }

    /// <summary>
    /// Create new car stock.
    /// </summary>
    /// <param name="carStock">The car stock entity <see cref="CarStock"/>.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="connection">The DB connection.</param>
    /// <param name="transaction">The DB transaction.</param>
    /// <returns>Returns true if the car stock was created, otherwise false.</returns>
    public async Task<bool> AddCarStockAsync(CarStock carStock, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = """
            INSERT INTO CarStocks (Id, DealerId, CarId, StockLevel, UnitPrice, UpdatedAt)
            VALUES (@Id, @DealerId, @CarId, @StockLevel, @UnitPrice, @UpdatedAt)
            """;

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        Id = carStock.Id.ToString(),
                        DealerId = carStock.DealerId.ToString(),
                        CarId = carStock.CarId.ToString(),
                        StockLevel = carStock.StockLevel,
                        UnitPrice = carStock.UnitPrice,
                        UpdatedAt = DateTimeOffset.UtcNow.ToString("O")
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            return rowsAffected > 0;
        }
        finally 
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Check if the car stock exists by given the dealer id and car id.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car stock exists, otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT 1 FROM CarStocks WHERE DealerId = @DealerId AND CarId = @CarId LIMIT 1";

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var exists = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        DealerId = dealerId.ToString(),
                        CarId = carId.ToString()
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            return exists.HasValue;
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Check if there is no stock left for this car for all dealers.
    /// </summary>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if there is no stock left for all dealers, otherwise false.</returns>
    public async Task<bool> ExistsAsync(Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = "SELECT 1 FROM CarStocks WHERE CarId = @CarId LIMIT 1";

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var exists = await connection.QuerySingleOrDefaultAsync<int?>(
                new CommandDefinition(
                    sql,
                    new
                    {
                        CarId = carId.ToString()
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            return exists.HasValue;
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
    }

    /// <summary>
    /// Remove car stock.
    /// </summary>
    /// <param name="dealerId">The id of the dealer.</param>
    /// <param name="carId">The id of the car.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Returns true if the car stock was removed, otherwise false.</returns>
    public async Task<bool> RemoveCarStockAsync(Guid dealerId, Guid carId, CancellationToken ct, IDbConnection? connection = null, IDbTransaction? transaction = null)
    {
        const string sql = "DELETE FROM CarStocks WHERE DealerId = @DealerId AND CarId = @CarId";

        var shouldDisposeConnection = connection is null;
        connection ??= await _connectionFactory.CreateConnectionAsync(ct);

        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                new CommandDefinition(
                    sql,
                    new
                    {
                        DealerId = dealerId.ToString(),
                        CarId = carId.ToString()
                    },
                    transaction: transaction,
                    cancellationToken: ct));

            return rowsAffected > 0;
        }
        finally
        {
            if (shouldDisposeConnection)
            {
                connection.Dispose();
            }
        }
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

    /// <summary>
    /// Car with stock row. Returns by the SQL query.
    /// </summary>
    public sealed class CarWithStockRow
    {
        public string Id { get; set; } = null!;
        public string DealerId { get; set; } = null!;
        public string CarStockId { get; set; } = null!;
        public string Make { get; set; } = null!;
        public string Model { get; set; } = null!;
        public int Year { get; set; }
        public decimal UnitPrice { get; set; }
        public int StockLevel { get; set; }
        public string CreatedAt { get; set; } = null!;
        public string StockUpdatedAt { get; set; } = null!;

        /// <summary>
        /// Map <see cref="CarWithStockRow"/> to <see cref="CarResponse"/>.
        /// </summary>
        /// <param name="row"></param>
        /// <returns></returns>
        public static CarResponse Map(CarWithStockRow row)
        {
            return new CarResponse
            {
                Id = Guid.Parse(row.Id),
                DealerId = Guid.Parse(row.DealerId),
                CarStockId = Guid.Parse(row.CarStockId),
                Make = row.Make,
                Model = row.Model,
                Year = row.Year,
                UnitPrice = row.UnitPrice,
                StockLevel = row.StockLevel,
                CreatedAt = DateTimeOffset.Parse(row.CreatedAt),
                StockUpdatedAt = DateTimeOffset.Parse(row.StockUpdatedAt)
            };
        }
    }
}
