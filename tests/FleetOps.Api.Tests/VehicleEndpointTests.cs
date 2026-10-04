using System.Net;
using System.Net.Http.Json;
using FleetOps.Application;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetOps.Api.Tests;

public class VehicleEndpointTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<string> RegisterAsync(string plate, int km)
    {
        var response = await _client.PostAsJsonAsync("/vehicles", new { plate, kilometres = km });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return response.Headers.Location!.ToString().Split('/').Last();
    }

    [Fact]
    public async Task Register_ReturnsCreatedWithLocation()
    {
        var id = await RegisterAsync("api-1", 0);
        Assert.True(Guid.TryParseExact(id, "N", out _) || Guid.TryParse(id, out _));
    }

    [Fact]
    public async Task LogMaintenance_ForUnknownVehicle_Returns404()
    {
        var response = await _client.PostAsJsonAsync($"/vehicles/{Guid.NewGuid()}/maintenance", new { description = "oil" });
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LogMaintenance_ForKnownVehicle_Returns204()
    {
        var id = await RegisterAsync("api-2", 100);
        var response = await _client.PostAsJsonAsync($"/vehicles/{id}/maintenance", new { description = "tyres" });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Due_ListsVehiclesOverTheMileageInterval()
    {
        await RegisterAsync("api-due", 25_000);
        var due = await _client.GetFromJsonAsync<List<VehicleDto>>("/vehicles/due");
        Assert.Contains(due!, v => v.Plate == "API-DUE");
    }

    [Fact]
    public async Task Due_ExcludesLowMileageVehicles()
    {
        await RegisterAsync("api-fresh", 10);
        var due = await _client.GetFromJsonAsync<List<VehicleDto>>("/vehicles/due");
        Assert.DoesNotContain(due!, v => v.Plate == "API-FRESH");
    }

    [Fact]
    public async Task Swagger_IsServed()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
