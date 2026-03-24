using CarManagement.Common.Exceptions;
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
}
