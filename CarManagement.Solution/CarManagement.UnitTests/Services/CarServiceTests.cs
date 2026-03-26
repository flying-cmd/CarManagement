using System.Data;
using CarManagement.Common.Exceptions;
using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using CarManagement.Repository.Repositories;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Mappers;
using CarManagement.Service.Services;
using CarManagementApi.Repository.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarManagement.UnitTests.Services;

using CarWithStockRow = CarRepository.CarWithStockRow;

public class CarServiceTests
{
    private readonly Mock<ICarRepository> _carRepositoryMock = new();
    private readonly Mock<IDealerRepository> _dealerRepositoryMock = new();
    private readonly Mock<ILogger<CarService>> _loggerMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly IDbConnection _connection = Mock.Of<IDbConnection>();
    private readonly IDbTransaction _transaction = Mock.Of<IDbTransaction>();
    private readonly CarService _sut;

    public CarServiceTests()
    {
        _unitOfWorkMock.SetupGet(x => x.Connection).Returns(_connection);
        _unitOfWorkMock.SetupGet(x => x.Transaction).Returns(_transaction);
        _unitOfWorkMock.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        _sut = new CarService(
            _carRepositoryMock.Object,
            _dealerRepositoryMock.Object,
            _loggerMock.Object,
            new CarMapper(),
            _unitOfWorkMock.Object);
    }

    private static Dealer CreateDealer()
    {
        return Dealer.Rehydrate(
            Guid.NewGuid(), 
            "Dealer One", 
            "dealer@example.com", 
            "0400000000", 
            "hashed", 
            DateTimeOffset.UtcNow);
    }

    private static Car CreateCar(
        Guid? id = null, 
        string make = "Audi", 
        string model = "A4", 
        int year = 2024)
    {
        return Car.Rehydrate(
            id ?? Guid.NewGuid(), 
            make, 
            model, 
            year, 
            DateTimeOffset.UtcNow);
    }

    private static CarWithStockRow CreateCarWithStockRow(Guid dealerId, string make = "Audi", string model = "A4")
    {
        return new CarWithStockRow
        {
            Id = Guid.NewGuid().ToString(),
            DealerId = dealerId.ToString(),
            CarStockId = Guid.NewGuid().ToString(),
            Make = make,
            Model = model,
            Year = 2024,
            UnitPrice = 42000m,
            StockLevel = 5,
            CreatedAt = DateTimeOffset.UtcNow.ToString("O"),
            StockUpdatedAt = DateTimeOffset.UtcNow.ToString("O")
        };
    }

    [Fact]
    public async Task AddCarAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var request = new AddCarRequestDto 
        { 
            Make = " Audi ", 
            Model = " A4 ", 
            Year = 2024, 
            StockLevel = 5, 
            UnitPrice = 42000m 
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.AddCarAsync(request, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCarAsync_WhenCarAlreadyExists_ShouldReuseExistingCar()
    {
        // Arrange
        var dealer = CreateDealer();
        var request = new AddCarRequestDto 
        { 
            Make = " Audi ", 
            Model = " A4 ", 
            Year = 2024, 
            StockLevel = 5, 
            UnitPrice = 42000m 
        };
        var existingCar = new Car("Audi", "A4", 2024);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealer.Id, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetByMakeModelYearAsync("Audi", "A4", 2024, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(existingCar);

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealer.Id, existingCar.Id, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        _carRepositoryMock
            .Setup(x => x.AddCarStockAsync(It.IsAny<CarStock>(), It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.AddCarAsync(request, dealer.Id, CancellationToken.None);

        // Assert
        result.Id.Should().Be(existingCar.Id);
        result.Make.Should().Be("Audi");
        result.Model.Should().Be("A4");

        _dealerRepositoryMock.Verify(x => x.GetDealerByIdAsync(dealer.Id, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.GetByMakeModelYearAsync("Audi", "A4", 2024, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>(), _connection, _transaction), Times.Never);
        _carRepositoryMock.Verify(x => x.AddCarStockAsync(It.IsAny<CarStock>(), It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCarAsync_WhenDealerAlreadyHasStockForCar_ShouldThrowBadRequestAndRollback()
    {
        // Arrange
        var dealer = CreateDealer();
        var existingCar = new Car("Toyota", "Corolla", 2020);

        var req = new AddCarRequestDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            StockLevel = 5,
            UnitPrice = 35000m
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealer.Id, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetByMakeModelYearAsync("Toyota", "Corolla", 2020, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(existingCar);

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealer.Id, existingCar.Id, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        // Act
        var act = () => _sut.AddCarAsync(req, dealer.Id, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ex.Which.Message.Should().Be("Car Stock already exists");

        _dealerRepositoryMock.Verify(x => x.GetDealerByIdAsync(dealer.Id, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.GetByMakeModelYearAsync("Toyota", "Corolla", 2020, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>(), _connection, _transaction), Times.Never);
        _carRepositoryMock.Verify(x => x.ExistsAsync(dealer.Id, existingCar.Id, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.AddCarStockAsync(It.IsAny<CarStock>(), It.IsAny<CancellationToken>(), _connection, _transaction), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCarAsync_WhenRequestIsValid_ShouldCreateCarStockCommitAndReturnResponse()
    {
        var dealerId = Guid.NewGuid();
        var request = new AddCarRequestDto { Make = " Audi ", Model = " A4 ", Year = 2024, StockLevel = 5, UnitPrice = 42000m };
        Car? savedCar = null;
        CarStock? savedCarStock = null;

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(CreateDealer());
        _carRepositoryMock
            .Setup(x => x.GetByMakeModelYearAsync("Audi", "A4", 2024, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync((Car?)null);
        _carRepositoryMock
            .Setup(x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>(), _connection, _transaction))
            .Callback<Car, CancellationToken, IDbConnection?, IDbTransaction?>((car, _, _, _) => savedCar = car)
            .ReturnsAsync(true);
        _carRepositoryMock
            .Setup(x => x.AddCarStockAsync(It.IsAny<CarStock>(), It.IsAny<CancellationToken>(), _connection, _transaction))
            .Callback<CarStock, CancellationToken, IDbConnection?, IDbTransaction?>((carStock, _, _, _) => savedCarStock = carStock)
            .ReturnsAsync(true);

        var result = await _sut.AddCarAsync(request, dealerId, CancellationToken.None);

        savedCar.Should().NotBeNull();
        savedCarStock.Should().NotBeNull();
        savedCar!.Make.Should().Be("Audi");
        savedCar.Model.Should().Be("A4");
        savedCarStock!.DealerId.Should().Be(dealerId);
        savedCarStock.CarId.Should().Be(savedCar.Id);
        result.Id.Should().Be(savedCar.Id);
        result.CarStockId.Should().Be(savedCarStock.Id);
        result.Make.Should().Be("Audi");
        result.Model.Should().Be("A4");
        result.UnitPrice.Should().Be(42000m);
        result.StockLevel.Should().Be(5);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddCarAsync_WhenCreatingNewCarFails_ShouldRollbackAndThrow()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;

        var req = new AddCarRequestDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            StockLevel = 5,
            UnitPrice = 35000m
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetByMakeModelYearAsync("Toyota", "Corolla", 2020, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync((Car?)null);

        _carRepositoryMock
            .Setup(x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.AddCarAsync(req, dealerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApiException>();

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddCarAsync_WhenCreatingCarStockFails_ShouldRollbackAndThrow()
    {
        // Arrange
        var dealer = CreateDealer();
        var existingCar = new Car("Toyota", "Corolla", 2020);
        var dealerId = dealer.Id;

        var req = new AddCarRequestDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2020,
            StockLevel = 5,
            UnitPrice = 35000m
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetByMakeModelYearAsync("Toyota", "Corolla", 2020, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(existingCar);

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, existingCar.Id, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        _carRepositoryMock
            .Setup(x => x.AddCarStockAsync(It.IsAny<CarStock>(), It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        // Act
        var act = () => _sut.AddCarAsync(req, dealerId, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ApiException>();

        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCarsAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var request = new ListCarsRequestDto 
        { 
            PageNumber = 1, 
            PageSize = 10 
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.ListCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null), Times.Once);
    }

    [Fact]
    public async Task ListCarsAsync_WhenDealerExists_ShouldReturnPagedRows()
    {
        // Arrange
        var dealer = CreateDealer();
        var request = new ListCarsRequestDto { PageNumber = 1, PageSize = 10 };
        var row1 = CreateCarWithStockRow(dealer.Id, "Audi", "A4");
        var row2 = CreateCarWithStockRow(dealer.Id, "BMW", "X5");
        var pagedRows = new PagedResult<CarWithStockRow>
        {
            Items = new[] { row1, row2 },
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = 2
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealer.Id, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.ListCarsAsync(dealer.Id, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedRows);

        // Act
        var result = await _sut.ListCarsAsync(request, dealer.Id, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(Guid.Parse(row1.Id));
        result.Items[0].CarStockId.Should().Be(Guid.Parse(row1.CarStockId));
        result.Items[1].Make.Should().Be("BMW");
        result.Items[1].Model.Should().Be("X5");
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenDealerDoesNotExists_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenCarDoesNotExists_ShouldThrowNotFound()
    { 
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);
        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync((Car?)null);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenDealerDoesNotOwnCar_ShouldThrowForbiddenAndRollback()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(CreateCar(carId));

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.Message.Should().Be("You are not authorized to remove this car");

        _unitOfWorkMock.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenRemovingLastStock_ShouldRemoveCarAndCommit()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(CreateCar(carId));

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        _carRepositoryMock
            .Setup(x => x.RemoveCarStockAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(false);

        _carRepositoryMock
            .Setup(x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        // Act
        await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        _carRepositoryMock.Verify(x => x.RemoveCarStockAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenOtherStockStillExists_ShouldOnlyRemoveCarStockAndCommit()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(CreateCar(carId));

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        _carRepositoryMock
            .Setup(x => x.RemoveCarStockAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction))
            .ReturnsAsync(true);

        // Act
        await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        _carRepositoryMock.Verify(x => x.RemoveCarStockAsync(dealerId, carId, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Once);
        _carRepositoryMock.Verify(x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>(), _connection, _transaction), Times.Never);
        _unitOfWorkMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var request = new SearchCarRequestDto 
        { 
            Make = " Audi ", 
            Model = " A4 ", 
            PageNumber = 1, 
            PageSize = 10 
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task SearchCarsAsync_WhenDealerExists_ShouldReturnPagedRows()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var req = new SearchCarRequestDto
        {
            Make = " Aud ",
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var row = CreateCarWithStockRow(dealerId);
        var pagedRows = new PagedResult<CarWithStockRow> 
        { 
            Items = new[] { row }, 
            PageNumber = 1, 
            PageSize = 10, 
            TotalCount = 1 
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, "Aud", "A4", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedRows);

        // Act
        var result = await _sut.SearchCarsAsync(req, dealerId, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(req.PageNumber);
        result.PageSize.Should().Be(req.PageSize);
        result.TotalCount.Should().Be(1);
        result.Items.Count.Should().Be(1);
        result.Items[0].Id.Should().Be(Guid.Parse(row.Id));
        result.Items[0].DealerId.Should().Be(dealerId);
        result.Items[0].UnitPrice.Should().Be(row.UnitPrice);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeIsNull_ShouldReturnMatchingPagedRows()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var req = new SearchCarRequestDto
        {
            Make = null,
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var row = CreateCarWithStockRow(dealerId);
        var pagedRows = new PagedResult<CarWithStockRow>
        {
            Items = new[] { row },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, null, "A4", 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedRows);

        // Act
        var result = await _sut.SearchCarsAsync(req, dealerId, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(req.PageNumber);
        result.PageSize.Should().Be(req.PageSize);
        result.TotalCount.Should().Be(1);
        result.Items.Count.Should().Be(1);
        result.Items[0].Id.Should().Be(Guid.Parse(row.Id));
        result.Items[0].DealerId.Should().Be(dealerId);
        result.Items[0].UnitPrice.Should().Be(row.UnitPrice);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenModelIsNull_ShouldReturnMatchingPagedRows()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var req = new SearchCarRequestDto
        {
            Make = " Audi ",
            Model = null,
            PageNumber = 1,
            PageSize = 10
        };
        var row = CreateCarWithStockRow(dealerId);
        var pagedRows = new PagedResult<CarWithStockRow>
        {
            Items = new[] { row },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, "Audi", null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedRows);

        // Act
        var result = await _sut.SearchCarsAsync(req, dealerId, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(req.PageNumber);
        result.PageSize.Should().Be(req.PageSize);
        result.TotalCount.Should().Be(1);
        result.Items.Count.Should().Be(1);
        result.Items[0].Id.Should().Be(Guid.Parse(row.Id));
        result.Items[0].DealerId.Should().Be(dealerId);
        result.Items[0].UnitPrice.Should().Be(row.UnitPrice);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeAndModelAreNull_ShouldReturnMatchingPagedRows()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var req = new SearchCarRequestDto
        {
            Make = null,
            Model = null,
            PageNumber = 1,
            PageSize = 10
        };
        var row = CreateCarWithStockRow(dealerId);
        var pagedRows = new PagedResult<CarWithStockRow>
        {
            Items = new[] { row },
            PageNumber = 1,
            PageSize = 10,
            TotalCount = 1
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, null, null, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedRows);

        // Act
        var result = await _sut.SearchCarsAsync(req, dealerId, CancellationToken.None);

        // Assert
        result.PageNumber.Should().Be(req.PageNumber);
        result.PageSize.Should().Be(req.PageSize);
        result.TotalCount.Should().Be(1);
        result.Items.Count.Should().Be(1);
        result.Items[0].Id.Should().Be(Guid.Parse(row.Id));
        result.Items[0].DealerId.Should().Be(dealerId);
        result.Items[0].UnitPrice.Should().Be(row.UnitPrice);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, 9, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenCarDoesNotExist_ShouldThrowNotFound()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync((Car?)null);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, 9, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenDealerDoesNotOwnCar_ShouldThrowForbidden()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(CreateCar(carId));

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, carId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, 9, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.Message.Should().Be("You are not authorized to update this car");
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenRepositoryReturnsTrue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var dealer = CreateDealer();
        var dealerId = dealer.Id;
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(dealer);

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(CreateCar(carId));

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(dealerId, carId, It.IsAny<CancellationToken>(), null, null))
            .ReturnsAsync(true);

        _carRepositoryMock
            .Setup(x => x.UpdateCarStockLevelAsync(carId, dealerId, 9, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.UpdateCarStockLevelByIdAsync(carId, 9, dealerId, CancellationToken.None);

        // Assert
        _carRepositoryMock.Verify(x => x.UpdateCarStockLevelAsync(carId, dealerId, 9, It.IsAny<CancellationToken>()), Times.Once);
    }
}
