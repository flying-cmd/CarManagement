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
    public async Task AddCarAsync_AndGetByMakeModelYearAsync_ShouldPersistCar()
    {
        // Arrange
        var suffix = Guid.NewGuid().ToString("N")[..6];
        var car = new Car($"Make{suffix}", $"Model{suffix}", 2030);

        // Act
        var added = await _fixture.CarRepository.AddCarAsync(car, CancellationToken.None);
        var loaded = await _fixture.CarRepository.GetByMakeModelYearAsync(car.Make, car.Model, car.Year, CancellationToken.None);

        // Assert
        added.Should().BeTrue();
        loaded.Should().NotBeNull();
        loaded!.Id.Should().Be(car.Id);
        loaded.Make.Should().Be(car.Make);
        loaded.Model.Should().Be(car.Model);
        loaded.Year.Should().Be(car.Year);
    }

    [Fact]
    public async Task AddCarStockAsync_AndExistsAsync_ShouldTrackDealerAndGlobalStock()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = await _fixture.CreateCarAsync(year: 2031);

        // Act
        var carStock = await _fixture.CreateCarStockAsync(dealer.Id, car.Id, stockLevel: 4, unitPrice: 31000m);

        // Assert
        var dealerOwnsStock = await _fixture.CarRepository.ExistsAsync(dealer.Id, car.Id, CancellationToken.None);
        var anyStockExists = await _fixture.CarRepository.ExistsAsync(car.Id, CancellationToken.None);

        dealerOwnsStock.Should().BeTrue();
        anyStockExists.Should().BeTrue();
        carStock.DealerId.Should().Be(dealer.Id);
        carStock.CarId.Should().Be(car.Id);
    }

    [Fact]
    public async Task ListCarsAsync_ShouldReturnDealerCarsOrderedByMakeModelYear()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var otherDealer = await _fixture.CreateDealerAsync();

        var bmwX3 = new Car("BMW", "X3", 2032);
        var bmwX1 = new Car("BMW", "X1", 2031);
        var toyotaCamry = new Car("Toyota", "Camry", 2033);
        var otherDealerCar = new Car("Ford", "Focus", 2034);

        await _fixture.CarRepository.AddCarAsync(bmwX3, CancellationToken.None);
        await _fixture.CarRepository.AddCarAsync(bmwX1, CancellationToken.None);
        await _fixture.CarRepository.AddCarAsync(toyotaCamry, CancellationToken.None);
        await _fixture.CarRepository.AddCarAsync(otherDealerCar, CancellationToken.None);

        await _fixture.CreateCarStockAsync(dealer.Id, bmwX3.Id, stockLevel: 1, unitPrice: 55000m);
        await _fixture.CreateCarStockAsync(dealer.Id, bmwX1.Id, stockLevel: 2, unitPrice: 45000m);
        await _fixture.CreateCarStockAsync(dealer.Id, toyotaCamry.Id, stockLevel: 3, unitPrice: 35000m);
        await _fixture.CreateCarStockAsync(otherDealer.Id, otherDealerCar.Id, stockLevel: 4, unitPrice: 25000m);

        // Act
        var result = await _fixture.CarRepository.ListCarsAsync(dealer.Id, pageNumber: 1, pageSize: 10, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(3);
        result.Items.Select(x => $"{x.Make}-{x.Model}-{x.Year}")
            .Should().Equal("BMW-X1-2031", "BMW-X3-2032", "Toyota-Camry-2033");
        result.Items.Should().OnlyContain(x => x.DealerId == dealer.Id.ToString());
    }

    [Fact]
    public async Task SearchCarsAsync_WhenFiltersAreProvided_ShouldReturnMatchingCars()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var alphaRoadster = new Car("Alpha", "Roadster", 2035);
        var alphaTruck = new Car("Alpha", "Truck", 2036);
        var betaRoadster = new Car("Beta", "Roadster", 2037);

        await _fixture.CarRepository.AddCarAsync(alphaRoadster, CancellationToken.None);
        await _fixture.CarRepository.AddCarAsync(alphaTruck, CancellationToken.None);
        await _fixture.CarRepository.AddCarAsync(betaRoadster, CancellationToken.None);

        await _fixture.CreateCarStockAsync(dealer.Id, alphaRoadster.Id, stockLevel: 1, unitPrice: 50000m);
        await _fixture.CreateCarStockAsync(dealer.Id, alphaTruck.Id, stockLevel: 1, unitPrice: 40000m);
        await _fixture.CreateCarStockAsync(dealer.Id, betaRoadster.Id, stockLevel: 1, unitPrice: 45000m);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: "alp",
            model: "road",
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle();
        result.Items[0].Make.Should().Be("Alpha");
        result.Items[0].Model.Should().Be("Roadster");
    }

    [Fact]
    public async Task SearchCarsAsync_WhenFiltersAreNull_ShouldReturnAllDealerCars()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car1 = await _fixture.CreateCarAsync(year: 2038);
        var car2 = await _fixture.CreateCarAsync(year: 2039);

        await _fixture.CreateCarStockAsync(dealer.Id, car1.Id, stockLevel: 1, unitPrice: 25000m);
        await _fixture.CreateCarStockAsync(dealer.Id, car2.Id, stockLevel: 2, unitPrice: 26000m);

        // Act
        var result = await _fixture.CarRepository.SearchCarsAsync(
            dealer.Id,
            make: null,
            model: null,
            pageNumber: 1,
            pageSize: 10,
            CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);
        result.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateCarStockLevelAsync_ShouldPersistUpdatedStock()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var car = await _fixture.CreateCarAsync(year: 2040);

        await _fixture.CreateCarStockAsync(dealer.Id, car.Id, stockLevel: 4, unitPrice: 32000m);

        // Act
        var updated = await _fixture.CarRepository.UpdateCarStockLevelAsync(car.Id, dealer.Id, 9, CancellationToken.None);
        
        // Assert
        var listed = await _fixture.CarRepository.ListCarsAsync(dealer.Id, 1, 10, CancellationToken.None);

        updated.Should().BeTrue();
        listed.Items.Should().ContainSingle();
        listed.Items[0].StockLevel.Should().Be(9);
    }

    [Fact]
    public async Task RemoveCarStockAsync_ShouldDeleteOnlyThatDealersStock()
    {
        // Arrange
        var dealer = await _fixture.CreateDealerAsync();
        var otherDealer = await _fixture.CreateDealerAsync();
        var car = await _fixture.CreateCarAsync(year: 2041);

        await _fixture.CreateCarStockAsync(dealer.Id, car.Id, stockLevel: 2, unitPrice: 20000m);
        await _fixture.CreateCarStockAsync(otherDealer.Id, car.Id, stockLevel: 3, unitPrice: 21000m);

        // Act
        var removed = await _fixture.CarRepository.RemoveCarStockAsync(dealer.Id, car.Id, CancellationToken.None);
        
        // Assert
        var dealerOwnsStock = await _fixture.CarRepository.ExistsAsync(dealer.Id, car.Id, CancellationToken.None);
        var otherDealerOwnsStock = await _fixture.CarRepository.ExistsAsync(otherDealer.Id, car.Id, CancellationToken.None);
        var anyStockExists = await _fixture.CarRepository.ExistsAsync(car.Id, CancellationToken.None);

        removed.Should().BeTrue();
        dealerOwnsStock.Should().BeFalse();
        otherDealerOwnsStock.Should().BeTrue();
        anyStockExists.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveCarByIdAsync_ShouldDeleteCar()
    {
        // Arrange
        var car = await _fixture.CreateCarAsync(year: 2042);

        // Act
        var removed = await _fixture.CarRepository.RemoveCarByIdAsync(car.Id, CancellationToken.None);
        
        // Assert
        var loaded = await _fixture.CarRepository.GetCarByIdAsync(car.Id, CancellationToken.None);

        removed.Should().BeTrue();
        loaded.Should().BeNull();
    }
}
