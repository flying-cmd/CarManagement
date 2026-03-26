using CarManagement.Common.Helpers;
using CarManagement.IntegrationTests.Infrastructure;
using CarManagement.Service.DTOs.Car;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;

namespace CarManagement.IntegrationTests.Api;

public sealed class CarApiIntegrationTests : IClassFixture<CarManagementWebApplicationFactory>
{
    private readonly CarManagementWebApplicationFactory _factory;

    public CarApiIntegrationTests(CarManagementWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCar_WhenDealerIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/cars", CreateAddCarRequest());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCar_WhenRequestIsInvalid_ShouldReturnValidationErrors()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = new AddCarRequestDto
        {
            Make = "",
            Model = " ",
            Year = 0,
            StockLevel = -1,
            UnitPrice = -1
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/cars", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().NotBeNullOrWhiteSpace();
        body.Errors.Should().NotBeNull();
        body.Errors!.Should().ContainKey("make");
        body.Errors.Should().ContainKey("model");
        body.Errors.Should().ContainKey("year");
        body.Errors.Should().ContainKey("stockLevel");
        body.Errors.Should().ContainKey("unitPrice");
    }

    [Fact]
    public async Task AddCar_WhenRequestIsValid_ShouldCreateCar()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/cars", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CarResponseDto>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeTrue();
        body.Message.Should().Be("Car added successfully.");
        body.Data.Should().NotBeNull();
        body.Data!.Make.Should().Be(request.Make);
        body.Data.Model.Should().Be(request.Model);
        body.Data.Year.Should().Be(request.Year);
        body.Data.StockLevel.Should().Be(request.StockLevel);
        body.Data.UnitPrice.Should().Be(request.UnitPrice);
    }

    [Fact]
    public async Task RemoveCar_WhenDealerIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/cars/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveCar_WhenDealerOwnsCar_ShouldDeleteIt()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var addedCar = await AddCarAsync(client);

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/cars/{addedCar.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var searchResponse = await client.GetAsync($"/api/cars/search?make={addedCar.Make}&model={addedCar.Model}&pageNumber=1&pageSize=10");
        var searchBody = await searchResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        searchBody.Should().NotBeNull();
        searchBody!.Data.Should().NotBeNull();
        searchBody.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task RemoveCar_WhenCarBelongsToAnotherDealer_ShouldReturnForbidden()
    {
        // Arrange
        var owner = await _factory.RegisterDealerAsync();
        var currentDealer = await _factory.RegisterDealerAsync();
        using var ownerClient = _factory.CreateAuthorizedClient(owner.AccessToken);
        using var currentDealerClient = _factory.CreateAuthorizedClient(currentDealer.AccessToken);
        var addedCar = await AddCarAsync(ownerClient);

        // Act
        var response = await currentDealerClient.DeleteAsync($"/api/cars/{addedCar.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("You are not authorized to remove this car");
    }

    [Fact]
    public async Task ListCars_ShouldReturnOnlyCurrentDealersCars()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var otherDealer = await _factory.RegisterDealerAsync();
        using var dealerClient = _factory.CreateAuthorizedClient(dealer.AccessToken);
        using var otherDealerClient = _factory.CreateAuthorizedClient(otherDealer.AccessToken);
        var dealersCar = await AddCarAsync(dealerClient);
        await AddCarAsync(otherDealerClient);

        // Act
        var response = await dealerClient.GetAsync("/api/cars?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalCount.Should().Be(1);
        body.Data.Items.Should().ContainSingle(x => x.Id == dealersCar.Id);
    }

    [Fact]
    public async Task SearchCars_WhenFiltersMatch_ShouldReturnMatchingCar()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();
        var addedCar = await AddCarAsync(client, request);

        // Act
        var response = await client.GetAsync($"/api/cars/search?make={request.Make}&model={request.Model}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id);
    }

    [Fact]
    public async Task SearchCars_WhenFiltersDoNotMatch_ShouldReturnEmptyResult()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();
        await AddCarAsync(client, request);

        // Act
        var response = await client.GetAsync($"/api/cars/search?make={Guid.NewGuid()}&model={Guid.NewGuid()}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task SearchCars_WhenMakeIsNull_ShouldResturnMatchingModel()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();
        var addedCar = await AddCarAsync(client, request);

        // Act
        var response = await client.GetAsync($"/api/cars/search?make={null}&model={request.Model}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id);
    }

    [Fact]
    public async Task SearchCars_WhenModelIsNull_ShouldResturnMatchingMake()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();
        var addedCar = await AddCarAsync(client, request);

        // Act
        var response = await client.GetAsync($"/api/cars/search?make={request.Make}&model={null}&pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();
        body.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id);
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenDealerIsNotAuthenticated_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();
        var carId = Guid.NewGuid();
        var request = new UpdateCarStockLevelRequestDto
        {
            Id = carId,
            StockLevel = 7
        };

        // Act
        var response = await client.PatchAsJsonAsync($"/api/cars/{carId}/stock-level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenDealerDoesNotOwnCar_ShouldReturnForbidden()
    {
        // Arrange
        var owner = await _factory.RegisterDealerAsync();
        var currentDealer = await _factory.RegisterDealerAsync();
        using var ownerClient = _factory.CreateAuthorizedClient(owner.AccessToken);
        using var currentDealerClient = _factory.CreateAuthorizedClient(currentDealer.AccessToken);
        var addedCar = await AddCarAsync(ownerClient);
        var request = new UpdateCarStockLevelRequestDto
        {
            Id = addedCar.Id,
            StockLevel = 7
        };

        // Act
        var response = await currentDealerClient.PatchAsJsonAsync($"/api/cars/{addedCar.Id}/stock-level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("You are not authorized to update this car");
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenDealerOwnsCar_ShouldUpdateStockLevel()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var addedCar = await AddCarAsync(client);
        var request = new UpdateCarStockLevelRequestDto
        {
            Id = addedCar.Id,
            StockLevel = 7
        };

        // Act
        var patchResponse = await client.PatchAsJsonAsync($"/api/cars/{addedCar.Id}/stock-level", request);

        // Assert
        patchResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var listResponse = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");
        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        listBody.Should().NotBeNull();
        listBody!.Data.Should().NotBeNull();
        listBody.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id && x.StockLevel == 7);
    }

    /// <summary>
    /// Creates a new <see cref="AddCarRequestDto"/>.
    /// </summary>
    /// <returns>Returns a new <see cref="AddCarRequestDto"/>.</returns>
    private static AddCarRequestDto CreateAddCarRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return new AddCarRequestDto
        {
            Make = $"Toyota{suffix}",
            Model = $"Corolla{suffix}",
            Year = 2024,
            StockLevel = 3,
            UnitPrice = 25000m
        };
    }

    /// <summary>
    /// Helper method to add a car.
    /// </summary>
    /// <param name="client">The http client.</param>
    /// <param name="request">The add car request <see cref="AddCarRequestDto"/>.</param>
    /// <returns>Returns <see cref="CarResponseDto"/>.</returns>
    private static async Task<CarResponseDto> AddCarAsync(HttpClient client, AddCarRequestDto? request = null)
    {
        request ??= CreateAddCarRequest();

        var response = await client.PostAsJsonAsync("/api/cars", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var body = await response.Content.ReadFromJsonAsync<ApiResponse<CarResponseDto>>();
        body.Should().NotBeNull();
        body!.Data.Should().NotBeNull();

        return body.Data!;
    }
}
