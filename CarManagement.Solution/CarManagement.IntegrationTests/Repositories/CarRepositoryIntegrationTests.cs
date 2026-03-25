using CarManagement.IntegrationTests.Infrastructure;
using CarManagement.Models.Entities;
using FluentAssertions;

namespace CarManagement.IntegrationTests.Repositories;

public sealed class CarRepositoryIntegrationTests : IClassFixture<RepositoryTestFixture>
{
    private readonly RepositoryTestFixture _fixture;

    public CarRepositoryIntegrationTests(RepositoryTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task AddCarAsync_ShouldPersistCar()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3);

        // Act
        await _fixture.CarRepository.AddCarAsync(car, CancellationToken.None);

        // Assert
        var exists = await _fixture.CarRepository.ExistsAsync(
            dealer.Id,
            "Toyota",
            "Corolla",
            2024,
            "Blue",
            CancellationToken.None);

        var loaded = await _fixture.CarRepository.GetCarByIdAsync(car.Id, CancellationToken.None);

        exists.Should().BeTrue();
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(car.Id);
        loaded.DealerId.Should().Be(dealer.Id);
        loaded.Make.Should().Be("Toyota");
        loaded.Model.Should().Be("Corolla");
        loaded.Year.Should().Be(2024);
        loaded.Colour.Should().Be("Blue");
        loaded.Price.Should().Be(25000m);
        loaded.StockLevel.Should().Be(3);
    }

    [Fact]
    public async Task ExistsAsync_WhenCarExists_ShouldReturnTrue()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3);
        await _fixture.CarRepository.AddCarAsync(car, CancellationToken.None);

        // Act
        var exists = await _fixture.CarRepository.ExistsAsync(
            dealer.Id,
            "Toyota",
            "Corolla",
            2024,
            "Blue",
            CancellationToken.None);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetCarByIdAsync_WhenCarExists_ShouldReturnCar()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3);
        await _fixture.CarRepository.AddCarAsync(car, CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.GetCarByIdAsync(car.Id, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(car.Id);
        result.DealerId.Should().Be(dealer.Id);
        result.Make.Should().Be("Toyota");
        result.Model.Should().Be("Corolla");
        result.Year.Should().Be(2024);
        result.Colour.Should().Be("Blue");
        result.Price.Should().Be(25000m);
        result.StockLevel.Should().Be(3);
    }

    [Fact]
    public async Task GetCarByIdAsync_WhenCarDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var carId = Guid.NewGuid();

        // Act
        var result = await _fixture.CarRepository.GetCarByIdAsync(carId, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListCarsAsync_ShouldReturnPageResultOfCar()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Camry", 2024, "Silver", 35000m, 1),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "BMW", "X3", 2023, "Black", 55000m, 1),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "BMW", "X1", 2022, "White", 45000m, 1),
            CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.ListCarsAsync(
            dealer.Id,
            pageNumber: 1,
            pageSize: 2,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(2);
        result.Items.Select(x => $"{x.Make}-{x.Model}")
            .Should().Equal("BMW-X1", "BMW-X3");
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeAndModelAreNotNull_ShouldReturnPageResultOfCar()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Camry", 2023, "Red", 28000m, 2),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Ford", "Focus", 2022, "White", 18000m, 1),
            CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: "toy",
            model: "cor",
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Make.Should().Be("Toyota");
        result.Items.Single().Model.Should().Be("Corolla");
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeAndModelAreNull_ShouldReturnAllCars()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Camry", 2023, "Red", 28000m, 2),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Ford", "Focus", 2022, "White", 18000m, 1),
            CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: null,
            model: null,
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Should().HaveCount(3);
    }

    [Fact]
    public async Task SearchCarsAsync_WhenModelIsNull_ShouldReturnMatchingCars()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Camry", 2023, "Red", 28000m, 2),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Ford", "Focus", 2022, "White", 18000m, 1),
            CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: null,
            model: "cor",
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items.Single().Make.Should().Be("Toyota");
        result.Items.Single().Model.Should().Be("Corolla");
    }

    [Fact]
    public async Task SearchCarsAsync_WhenMakeIsNull_ShouldReturnMatchingCars()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Corolla", 2024, "Blue", 25000m, 3),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Toyota", "Camry", 2023, "Red", 28000m, 2),
            CancellationToken.None);

        await _fixture.CarRepository.AddCarAsync(
            new Car(dealer.Id, "Ford", "Focus", 2022, "White", 18000m, 1),
            CancellationToken.None);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: "toy",
            model: null,
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
        result.Items.Should().Contain(x => x.Make == "Toyota" && x.Model == "Corolla");
        result.Items.Should().Contain(x => x.Make == "Toyota" && x.Model == "Camry");
    }

    [Fact]
    public async Task UpdateStockLevel_ShouldPersistChanges()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = new Car(dealer.Id, "Mazda", "CX5", 2024, "Grey", 32000m, 4);

        await _fixture.CarRepository.AddCarAsync(car, CancellationToken.None);

        // Act
        var updated = await _fixture.CarRepository.UpdateCarStockLevelByIdAsync(
            car.Id,
            9,
            CancellationToken.None);

        // Assert
        updated.Should().BeTrue();

        var loadedAfterUpdate = await _fixture.CarRepository.GetCarByIdAsync(car.Id, CancellationToken.None);
        loadedAfterUpdate.Should().NotBeNull();
        loadedAfterUpdate!.StockLevel.Should().Be(9);
    }
}
