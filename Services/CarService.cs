using CarInsurance.Api.Data;
using CarInsurance.Api.Dtos;
using CarInsurance.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace CarInsurance.Api.Services;

public class CarService(AppDbContext db)
{
    private readonly AppDbContext _db = db;

    public async Task<List<CarDto>> ListCarsAsync()
    {
        return await _db.Cars.Include(c => c.Owner)
            .Select(c => new CarDto(c.Id, c.Vin, c.Make, c.Model, c.YearOfManufacture,
                                    c.OwnerId, c.Owner.Name, c.Owner.Email))
            .ToListAsync();
    }

    public async Task<long> CreateCarAsync(CreateCarDto dto)
    {
        var ownerExists = await _db.Owners.AnyAsync(o => o.Id == dto.OwnerId);
        if (!ownerExists) throw new KeyNotFoundException($"Owner {dto.OwnerId} not found");
        
        var car = new Car
        {
            Vin = dto.Vin,
            Make = dto.Make,
            Model = dto.Model,
            YearOfManufacture = dto.Year,
            OwnerId = dto.OwnerId
        };

        _db.Cars.Add(car);
        await _db.SaveChangesAsync();

        return car.Id;
    }

    public async Task<bool> IsCarAlreadyRegistered(string vin)
    {
        return await _db.Cars.AnyAsync(c => c.Vin == vin);
    }

    public async Task<bool> IsInsuranceValidAsync(long carId, DateOnly date)
    {
        var carExists = await _db.Cars.AnyAsync(c => c.Id == carId);
        if (!carExists) throw new KeyNotFoundException($"Car {carId} not found");

        return await _db.Policies.AnyAsync(p =>
            p.CarId == carId &&
            p.StartDate <= date &&
            p.EndDate >= date
        );
    }
    
    public async Task<ClaimDto> RegisterClaimAsync(long carId, CreateClaimDto dto)
    {
        var car = await _db.Cars.FindAsync(carId);
        if (car is null)
            throw new KeyNotFoundException();

        var claim = new Claim
        {
            CarId = carId,
            ClaimDate = dto.ClaimDate,
            Description = dto.Description,
            Amount = dto.Amount
        };

        _db.Claims.Add(claim);
        await _db.SaveChangesAsync();

        return new ClaimDto(claim.Id, claim.ClaimDate, claim.Description, claim.Amount);
    }

    public async Task<CarHistoryDto> GetCarHistoryAsync(long carId)
    {
        var car = await _db.Cars
            .Include(c => c.Policies)
            .Include(c => c.Claims)
            .FirstOrDefaultAsync(c => c.Id == carId);

        if (car is null)
            throw new KeyNotFoundException();

        var policies = car.Policies
            .Select(p => new PolicyPeriodDto(p.Id, p.StartDate, p.EndDate, p.Provider))
            .OrderBy(p => p.StartDate)
            .ToList();

        var claims = car.Claims
            .Select(cl => new ClaimDto(cl.Id, cl.ClaimDate, cl.Description, cl.Amount))
            .OrderBy(cl => cl.ClaimDate)
            .ToList();

        return new CarHistoryDto(car.Id, policies, claims);
    }
}

