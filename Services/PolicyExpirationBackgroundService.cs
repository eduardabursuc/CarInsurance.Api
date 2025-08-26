using CarInsurance.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class PolicyExpirationBackgroundService(
    ILogger<PolicyExpirationBackgroundService> logger,
    IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        AppDbContext db = serviceScopeFactory.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckAndLogExpiredPoliciesAsync(db);
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking expired policies.");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); 
            }
        }
    }

    public async Task CheckAndLogExpiredPoliciesAsync(AppDbContext db, DateTime? now = null)
    {
        var currentTime = now ?? DateTime.UtcNow;
        
        using var scope = serviceScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var policies = await context.Policies
            .Include(p => p.Car)
            .Where(p => !p.IsProcessed &&
                        p.EndDate.ToDateTime(TimeOnly.MinValue) <= currentTime &&
                        p.EndDate.ToDateTime(TimeOnly.MinValue).AddHours(1) > currentTime)
            .ToListAsync();

        foreach (var policy in policies)
        {
            logger.LogInformation("Policy expiration alert: PolicyId={PolicyId}, Car={CarModel}, Provider={Provider}",
                policy.Id, policy.Car?.Model, policy.Provider);

            policy.IsProcessed = true;
        }

        await db.SaveChangesAsync();
    }
}