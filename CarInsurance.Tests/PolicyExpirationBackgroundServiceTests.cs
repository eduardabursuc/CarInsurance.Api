using CarInsurance.Api.Data;
using CarInsurance.Api.Models;
using CarInsurance.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CarInsurance.Tests;

// Fake logger
public class TestLogger<T> : ILogger<T>
{
    public List<string> LoggedMessages { get; } = new();
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        LoggedMessages.Add(formatter(state, exception));
    }
}

// Fake ServiceScopeFactory for testing
public class TestServiceScopeFactory(AppDbContext dbContext) : IServiceScopeFactory
{
    public IServiceScope CreateScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton(dbContext);
        var serviceProvider = services.BuildServiceProvider();
        return serviceProvider.CreateScope();
    }
}

public class PolicyExpirationBackgroundServiceTests
{
    private AppDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CheckAndLogExpiredPoliciesAsync_LogsPoliciesAndMarksAsProcessed()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var logger = new TestLogger<PolicyExpirationBackgroundService>();
        var serviceScopeFactory = new TestServiceScopeFactory(db);

        var testTime = DateTime.UtcNow.Date.AddDays(1).AddMinutes(30); // tomorrow 00:30
        var policy = new InsurancePolicy
        {
            Id = 1,
            EndDate = DateOnly.FromDateTime(testTime.Date), // midnight today
            Provider = "TestProvider",
            CarId = 1,
            Car = new Car { Id = 1, Model = "TestCar", Vin = "VIN1" },
            IsProcessed = false
        };
        db.Policies.Add(policy);
        await db.SaveChangesAsync();

        var service = new PolicyExpirationBackgroundService(logger, serviceScopeFactory);

        // Act
        await service.CheckAndLogExpiredPoliciesAsync(db, testTime);

        // Assert
        Assert.Single(logger.LoggedMessages);
        Assert.Contains("Policy expiration alert:", logger.LoggedMessages[0]);
        Assert.Contains("PolicyId=1", logger.LoggedMessages[0]);

        var updatedPolicy = await db.Policies.FindAsync(1L);
        Assert.True(updatedPolicy?.IsProcessed);
    }

    [Fact]
    public async Task CheckAndLogExpiredPoliciesAsync_DoesNotProcessTwice()
    {
        // Arrange
        var db = CreateInMemoryDb();
        var logger = new TestLogger<PolicyExpirationBackgroundService>();
        var serviceScopeFactory = new TestServiceScopeFactory(db);

        var testTime = DateTime.UtcNow.Date.AddDays(1).AddMinutes(30); // tomorrow 00:30
        var policy = new InsurancePolicy
        {
            Id = 2,
            EndDate = DateOnly.FromDateTime(testTime.Date),
            IsProcessed = true, // already processed
            Provider = "TestProvider",
            CarId = 2,
            Car = new Car { Id = 2, Model = "TestCar2", Vin = "VIN2" }
        };
        db.Policies.Add(policy);
        await db.SaveChangesAsync();

        var service = new PolicyExpirationBackgroundService(logger, serviceScopeFactory);

        // Act
        await service.CheckAndLogExpiredPoliciesAsync(db, testTime);

        // Assert
        Assert.Empty(logger.LoggedMessages);

        var updatedPolicy = await db.Policies.FindAsync(2L);
        Assert.True(updatedPolicy?.IsProcessed);
    }
}