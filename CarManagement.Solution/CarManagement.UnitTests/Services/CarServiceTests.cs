using CarManagement.Common.Exceptions;
using CarManagement.Common.Helpers;
using CarManagement.Models.Entities;
using CarManagement.Repository.Interfaces;
using CarManagement.Repository.Repositories;
using CarManagement.Service.DTOs.Car;
using CarManagement.Service.Services;
using CarManagementApi.Repository.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;

namespace CarManagement.UnitTests.Services;

public class CarServiceTests
{
    private readonly Mock<ICarRepository> _carRepositoryMock;
    private readonly Mock<IDealerRepository> _dealerRepositoryMock;
    private readonly Mock<ILogger<CarService>> _loggerMock;
    private readonly CarService _sut;
    public CarServiceTests()
    {
        _carRepositoryMock = new Mock<ICarRepository>();
        _dealerRepositoryMock = new Mock<IDealerRepository>();
        _loggerMock = new Mock<ILogger<CarService>>();

        _sut = new CarService(
            _carRepositoryMock.Object, 
            _dealerRepositoryMock.Object, 
            _loggerMock.Object,
            new Service.Mappers.CarMapper());
    }

    private static Dealer CreateDealer(
        string name = "Dealer One",
        string email = "dealer@example.com",
        string password = "P@ssword123!")
    {
        var passwordHasher = new PasswordHasher<Dealer>();
        return Dealer.CreateDealer(name, email, password, passwordHasher);
    }

    [Fact]
    public async Task AddCarAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var addCarRequest = new AddCarRequestDto
        {
            Make = "Audi",
            Model = "A4",
            Year = 2018,
            Colour = "White",
            Price = 20000m,
            StockLevel = 10
        };
        var dealerId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.AddCarAsync(addCarRequest, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddCarAsync_WhenCarAlreadyExists_ShouldThrowBadRequest()
    {
        // Arrange
        var addCarRequest = new AddCarRequestDto
        {
            Make = "Audi",
            Model = "A4",
            Year = 2018,
            Colour = "White",
            Price = 20000m,
            StockLevel = 10
        };
        var dealerId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(
                dealerId,
                addCarRequest.Make,
                addCarRequest.Model,
                addCarRequest.Year,
                addCarRequest.Colour,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var act = async () => await _sut.AddCarAsync(addCarRequest, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ex.Which.Message.Should().Be("Car already exists");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.ExistsAsync(
                dealerId,
                addCarRequest.Make,
                addCarRequest.Model,
                addCarRequest.Year,
                addCarRequest.Colour,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddCarAsync_WhenCarDoesNotExist_ShouldAddCar()
    {
        // Arrange
        var addCarRequest = new AddCarRequestDto
        {
            Make = "Audi",
            Model = "A4",
            Year = 2018,
            Colour = "White",
            Price = 20000m,
            StockLevel = 10
        };
        var dealerId = Guid.NewGuid();
        Car? addedCar = null;

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.ExistsAsync(
                dealerId,
                addCarRequest.Make,
                addCarRequest.Model,
                addCarRequest.Year,
                addCarRequest.Colour,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _carRepositoryMock
            .Setup(x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>()))
            .Callback<Car, CancellationToken>((car, _) => addedCar = car)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.AddCarAsync(addCarRequest, dealerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().NotBeEmpty();
        result.DealerId.Should().Be(dealerId);
        result.Make.Should().Be(addCarRequest.Make);
        result.Model.Should().Be(addCarRequest.Model);
        result.Year.Should().Be(addCarRequest.Year);
        result.Colour.Should().Be(addCarRequest.Colour);
        result.Price.Should().Be(addCarRequest.Price);
        result.StockLevel.Should().Be(addCarRequest.StockLevel);

        addedCar.Should().NotBeNull();
        addedCar.Id.Should().NotBeEmpty();
        addedCar.DealerId.Should().Be(dealerId);
        addedCar.Make.Should().Be(addCarRequest.Make);
        addedCar.Model.Should().Be(addCarRequest.Model);
        addedCar.Year.Should().Be(addCarRequest.Year);
        addedCar.Colour.Should().Be(addCarRequest.Colour);
        addedCar.Price.Should().Be(addCarRequest.Price);
        addedCar.StockLevel.Should().Be(addCarRequest.StockLevel);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.ExistsAsync(
                dealerId,
                addCarRequest.Make,
                addCarRequest.Model,
                addCarRequest.Year,
                addCarRequest.Colour,
                It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.AddCarAsync(It.IsAny<Car>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListCarsAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new ListCarsRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.ListCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.ListCarsAsync(dealerId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]

    public async Task ListCarsAsync_WhenDealerExists_ShouldListCars()
    {
        // Arrange
        var request = new ListCarsRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>
        {
            new Car(
                dealerId,
                "Audi",
                "A4",
                2018,
                "White",
                20000m,
                10),
            new Car(
                dealerId,
                "BMW",
                "X5",
                2019,
                "Black",
                25000m,
                5)
        };
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.ListCarsAsync(dealerId, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.ListCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);
        result.Items.Should().BeEquivalentTo(cars);
        result.Items[0].Id.Should().NotBeEmpty();
        result.Items[0].DealerId.Should().Be(dealerId);
        result.Items[0].Make.Should().Be(cars[0].Make);
        result.Items[0].Model.Should().Be(cars[0].Model);
        result.Items[0].Year.Should().Be(cars[0].Year);
        result.Items[0].Colour.Should().Be(cars[0].Colour);
        result.Items[0].Price.Should().Be(cars[0].Price);
        result.Items[0].StockLevel.Should().Be(cars[0].StockLevel);
        result.Items[1].Id.Should().NotBeEmpty();
        result.Items[1].DealerId.Should().Be(dealerId);
        result.Items[1].Make.Should().Be(cars[1].Make);
        result.Items[1].Model.Should().Be(cars[1].Model);
        result.Items[1].Year.Should().Be(cars[1].Year);
        result.Items[1].Colour.Should().Be(cars[1].Colour);
        result.Items[1].Price.Should().Be(cars[1].Price);
        result.Items[1].StockLevel.Should().Be(cars[1].StockLevel);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.ListCarsAsync(dealerId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ListCarsAsync_WhenNoCarsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new ListCarsRequestDto
        {
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>();
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.ListCarsAsync(dealerId, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.ListCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);
        result.Items.Should().BeEmpty();

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.ListCarsAsync(dealerId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenCarDoesNotExist_ShouldThrowNotFound()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Car?)null);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenCarDoesNotBelongToCurrentDealer_ShouldThrowForbidden()
    {
        // Arrange
        var currentDealerId = Guid.NewGuid();
        var anotherDealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var car = Car.Rehydrate(
            carId,
            anotherDealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(currentDealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, currentDealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.Message.Should().Be("You are not authorized to remove this car");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(currentDealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenRepositoryRemoveReturnsFalse_ShouldThrowNotFound()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var car = Car.Rehydrate(
            carId,
            dealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        _carRepositoryMock
            .Setup(x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RemoveCarByIdAsync_WhenRepositoryRemoveReturnsTrue_ShouldRemoveSuccessfully()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var car = Car.Rehydrate(
            carId,
            dealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        _carRepositoryMock
            .Setup(x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.RemoveCarByIdAsync(carId, dealerId, CancellationToken.None);

        // Assert
        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.RemoveCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = " Audi ",
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make, request.Model, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenDealerExists_ShouldReturnSearchedCars()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = " Audi ",
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>
        {
            new Car(
                dealerId,
                "Audi",
                "A4",
                2018,
                "White",
                20000m,
                10)
        };
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(cars);
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenNoCarsExist_ShouldReturnEmptyList()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = " Audi ",
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>();
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(cars);
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeIsNull_ShouldReturnSearchedCars()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = null,
            Model = " A4 ",
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>
        {
            new Car(
                dealerId,
                "Audi",
                "A4",
                2018,
                "White",
                20000m,
                10)
        };
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, request.Make, request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(cars);
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make, request.Model.Trim(), request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenModelIsNull_ShouldReturnSearchedCars()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = " Audi ",
            Model = null,
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>
        {
            new Car(
                dealerId,
                "Audi",
                "A4",
                2018,
                "White",
                20000m,
                10)
        };
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(cars);
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make.Trim(), request.Model, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeAndModelAreNull_ShouldReturnSearchedCars()
    {
        // Arrange
        var request = new SearchCarRequestDto
        {
            Make = null,
            Model = null,
            PageNumber = 1,
            PageSize = 10
        };
        var dealerId = Guid.NewGuid();
        var cars = new List<Car>
        {
            new Car(
                dealerId,
                "Audi",
                "A4",
                2018,
                "White",
                20000m,
                10)
        };
        var pagedCars = new PagedResult<Car>
        {
            Items = cars,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
            TotalCount = cars.Count
        };

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.SearchCarsAsync(dealerId, request.Make, request.Model, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedCars);

        // Act
        var result = await _sut.SearchCarsAsync(request, dealerId, CancellationToken.None);

        // Assert
        result.Items.Should().BeEquivalentTo(cars);
        result.PageNumber.Should().Be(request.PageNumber);
        result.PageSize.Should().Be(request.PageSize);
        result.TotalCount.Should().Be(cars.Count);

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.SearchCarsAsync(dealerId, request.Make, request.Model, request.PageNumber, request.PageSize, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenDealerDoesNotExist_ShouldThrowUnauthorized()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var newStockLevel = 10;

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Dealer?)null);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, newStockLevel, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status401Unauthorized);
        ex.Which.Message.Should().Be("Unauthorized");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenCarDoesNotExist_ShouldThrowNotFound()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var newStockLevel = 10;

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Car?)null);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, newStockLevel, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenCarDoesNotBelongToCurrentDealer_ShouldThrowForbidden()
    {
        // Arrange
        var currentDealerId = Guid.NewGuid();
        var anotherDealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var newStockLevel = 10;
        var car = Car.Rehydrate(
            carId,
            anotherDealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(currentDealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, newStockLevel, currentDealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ex.Which.Message.Should().Be("You are not authorized to update this car");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(currentDealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenRepositoryUpdateReturnsFalse_ShouldThrowNotFound()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var newStockLevel = 10;
        var car = Car.Rehydrate(
            carId,
            dealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        _carRepositoryMock
            .Setup(x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _sut.UpdateCarStockLevelByIdAsync(carId, newStockLevel, dealerId, CancellationToken.None);

        // Assert
        var ex = await act.Should().ThrowAsync<ApiException>();
        ex.Which.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        ex.Which.Message.Should().Be("Car not found");

        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateCarStockLevelByIdAsync_WhenRepositoryUpdateReturnsTrue_ShouldUpdateSuccessfully()
    {
        // Arrange
        var dealerId = Guid.NewGuid();
        var carId = Guid.NewGuid();
        var newStockLevel = 10;
        var car = Car.Rehydrate(
            carId,
            dealerId,
            "Audi",
            "A8",
            2022,
            "Red",
            10000m,
            10,
            DateTime.UtcNow,
            DateTime.UtcNow);

        _dealerRepositoryMock
            .Setup(x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDealer());

        _carRepositoryMock
            .Setup(x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(car);

        _carRepositoryMock
            .Setup(x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        await _sut.UpdateCarStockLevelByIdAsync(carId, newStockLevel, dealerId, CancellationToken.None);

        // Assert
        _dealerRepositoryMock.Verify(
            x => x.GetDealerByIdAsync(dealerId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.GetCarByIdAsync(carId, It.IsAny<CancellationToken>()),
            Times.Once);
        _carRepositoryMock.Verify(
            x => x.UpdateCarStockLevelByIdAsync(carId, newStockLevel, It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
