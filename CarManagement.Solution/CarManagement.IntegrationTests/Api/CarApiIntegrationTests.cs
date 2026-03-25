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
    public async Task AddCar_WhenTheDealerDoesNotLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        var request = new AddCarRequestDto
        {
            Make = "Toyota",
            Model = "Corolla",
            Year = 2024,
            Colour = "Blue",
            Price = 25000m,
            StockLevel = 3
        };

        using var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/cars", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AddCar_WhenRequestIsInvalid_ShouldReturnValidationError()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var request = new AddCarRequestDto
        {
            Make = "",
            Model = " ",
            Year = 2024,
            Colour = "Blue",
            Price = -13,
            StockLevel = 3
        };

        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);

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
        body.Errors.Should().ContainKey("price");
    }

    [Fact]
    public async Task AddCar_WhenRequestIsValid_ShouldCreateCar()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();

        // Act
        var addResponse = await client.PostAsJsonAsync("/api/cars", request);

        // Assert
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var addBody = await addResponse.Content.ReadFromJsonAsync<ApiResponse<CarResponseDto>>();
        addBody.Should().NotBeNull();
        addBody!.Data.Should().NotBeNull();
        addBody.Data!.Make.Should().Be(request.Make);
        addBody.Data.Model.Should().Be(request.Model);
        addBody.Data.StockLevel.Should().Be(request.StockLevel);
    }

    [Fact]
    public async Task RemoveCar_WhenTheDealerDoesNotLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        var carId = Guid.NewGuid();
        using var client = _factory.CreateClient();

        // Act
        var response = await client.DeleteAsync($"/api/cars/{carId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemoveCar_WhenTheCarDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        var carId = Guid.NewGuid();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);

        // Act
        var response = await client.DeleteAsync($"/api/cars/{carId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemoveCar_WhenTheDealerOwnsTheCar_ShouldDeleteCar()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();
        var addedCar = await AddCarAsync(client, request);

        // Act
        var deleteResponse = await client.DeleteAsync($"/api/cars/{addedCar.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var searchAfterDeleteResponse = await client.GetAsync($"/api/cars/search?make={request.Make}&model={request.Model}&pageNumber=1&pageSize=10");
        var searchAfterDeleteBody = await searchAfterDeleteResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        searchAfterDeleteBody.Should().NotBeNull();
        searchAfterDeleteBody!.Data.Should().NotBeNull();
        searchAfterDeleteBody.Data!.TotalCount.Should().Be(0);
        searchAfterDeleteBody.Data.Items.Should().BeEmpty();
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
        var deleteResponse = await currentDealerClient.DeleteAsync($"/api/cars/{addedCar.Id}");

        // Assert
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        var body = await deleteResponse.Content.ReadFromJsonAsync<ApiResponse<object?>>();

        body.Should().NotBeNull();
        body!.Success.Should().BeFalse();
        body.Message.Should().Be("You are not authorized to remove this car");
    }

    [Fact]
    public async Task ListCars_WhenTheDealerDoesNotLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        using var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListCars_WhenDealerHasCars_ShouldReturnDealerCars()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var addedCar = await AddCarAsync(client);

        // Act
        var listResponse = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        listBody.Should().NotBeNull();
        listBody!.Data.Should().NotBeNull();
        listBody.Data!.TotalCount.Should().Be(1);
        listBody.Data.Items.Should().ContainSingle(x => x.Id == addedCar.Id && x.StockLevel == addedCar.StockLevel);
    }

    [Fact]
    public async Task ListCars_WhenDealerHasNoCars_ShouldReturnEmptyList()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);

        // Act
        var listResponse = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");

        // Assert
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var listBody = await listResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        listBody.Should().NotBeNull();
        listBody!.Data.Should().NotBeNull();
        listBody.Data!.TotalCount.Should().Be(0);
        listBody.Data.Items.Should().BeEmpty();
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
        var searchResponse = await client.GetAsync($"/api/cars/search?make={request.Make}&model={request.Model}&pageNumber=1&pageSize=10");

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchBody = await searchResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        searchBody.Should().NotBeNull();
        searchBody!.Data.Should().NotBeNull();
        searchBody.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id);
    }

    [Fact]
    public async Task SearchCars_WhenFiltersDoNotMatch_ShouldReturnEmptyList()
    {
        // Arrange
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);
        var request = CreateAddCarRequest();

        // Act
        var searchResponse = await client.GetAsync($"/api/cars/search?make=12345&model=6789&pageNumber=1&pageSize=10");

        // Assert
        searchResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var searchBody = await searchResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();
        searchBody.Should().NotBeNull();
        searchBody!.Data.Should().NotBeNull();
        searchBody.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenTheDealerDoesNotLogin_ShouldReturnUnauthorized()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var request = new UpdateCarStockLevelRequestDto
        {
            Id = carId,
            StockLevel = 7
        };

        using var client = _factory.CreateClient();

        // Act
        var response = await client.PatchAsJsonAsync($"/api/cars/{carId}/stock-level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenTheCarDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var carId = Guid.NewGuid();
        var request = new UpdateCarStockLevelRequestDto
        {
            Id = carId,
            StockLevel = 7
        };
        var dealer = await _factory.RegisterDealerAsync();
        using var client = _factory.CreateAuthorizedClient(dealer.AccessToken);

        // Act
        var response = await client.PatchAsJsonAsync($"/api/cars/{carId}/stock-level", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenTheDealerDoesNotOwnTheCar_ShouldReturnForbidden()
    {
        // Arrange
        var owner = await _factory.RegisterDealerAsync();
        var currentDealer = await _factory.RegisterDealerAsync();
        var ownerClient = _factory.CreateAuthorizedClient(owner.AccessToken);
        var currentDealerClient = _factory.CreateAuthorizedClient(currentDealer.AccessToken);

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
        body.Errors.Should().NotBeNull();
        body.Errors!["Error"].Should().ContainSingle("You are not authorized to update this car");
    }

    [Fact]
    public async Task UpdateCarStockLevel_WhenTheDealerOwnsTheCar_ShouldUpdateStockLevel()
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

        var listAfterPatchResponse = await client.GetAsync("/api/cars?pageNumber=1&pageSize=10");
        var listAfterPatchBody = await listAfterPatchResponse.Content.ReadFromJsonAsync<ApiResponse<PagedResult<CarResponseDto>>>();

        listAfterPatchBody.Should().NotBeNull();
        listAfterPatchBody!.Data.Should().NotBeNull();
        listAfterPatchBody.Data!.Items.Should().ContainSingle(x => x.Id == addedCar.Id && x.StockLevel == request.StockLevel);
    }

    private static AddCarRequestDto CreateAddCarRequest()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        return new AddCarRequestDto
        {
            Make = $"Toyota{suffix}",
            Model = $"Corolla{suffix}",
            Year = 2024,
            Colour = "Blue",
            Price = 25000m,
            StockLevel = 3
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
        if (request is null)
        {
            request = CreateAddCarRequest();
        }

        var addResponse = await client.PostAsJsonAsync("/api/cars", request);
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var addBody = await addResponse.Content.ReadFromJsonAsync<ApiResponse<CarResponseDto>>();
        addBody.Should().NotBeNull();
        addBody!.Data.Should().NotBeNull();

        return addBody.Data!;
    }
}
