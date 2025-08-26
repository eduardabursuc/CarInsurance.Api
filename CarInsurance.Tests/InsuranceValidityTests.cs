using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace CarInsurance.Tests;

public class InsuranceValidityTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("2024-01-01")]
    [InlineData("2024-12-31")]
    [InlineData("2024-02-29")] // leap year
    [InlineData("2000-02-29")] // century leap year
    [InlineData("1900-01-01")] // boundary year
    [InlineData("2099-12-31")] // future boundary
    public async Task Valid_date_returns_200(string date)
    {
        // Arrange
        const int existingCarId = 1;

        // Act
        var response = await _client.GetAsync($"/api/cars/{existingCarId}/insurance-valid?date={date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("bad-date")]
    [InlineData("2024/01/01")] // wrong separator
    [InlineData("01-01-2024")] // wrong order
    [InlineData("2024-1-1")] // single digits
    [InlineData("24-01-01")] // two digit year
    [InlineData("2024-02-30")] // impossible date
    [InlineData("2024-13-01")] // invalid month
    [InlineData("2024-01-32")] // invalid day
    [InlineData("2023-02-29")] // non-leap year Feb 29
    [InlineData("1900-02-29")] // century non-leap year Feb 29
    [InlineData("")] // empty
    [InlineData(" ")] // whitespace
    public async Task Invalid_date_returns_400(string date)
    {
        // Arrange
        const int existingCarId = 1;

        // Act
        var response = await _client.GetAsync($"/api/cars/{existingCarId}/insurance-valid?date={date}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Missing_date_parameter_returns_400()
    {
        // Arrange
        const int existingCarId = 1;

        // Act
        var response = await _client.GetAsync($"/api/cars/{existingCarId}/insurance-valid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(9999)]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task Invalid_car_id_returns_404_or_400(int carId)
    {
        // Arrange
        const string validDate = "2024-01-01";

        // Act
        var response = await _client.GetAsync($"/api/cars/{carId}/insurance-valid?date={validDate}");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);
    }
}