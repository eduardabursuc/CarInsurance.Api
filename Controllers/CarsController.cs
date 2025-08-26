using CarInsurance.Api.Dtos;
using CarInsurance.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace CarInsurance.Api.Controllers;

[ApiController]
[Route("api")]
public class CarsController(CarService service) : ControllerBase
{
    private readonly CarService _service = service;

    [HttpGet("cars")]
    public async Task<ActionResult<List<CarDto>>> GetCars()
        => Ok(await _service.ListCarsAsync());

    [HttpGet("cars/{carId:long}/insurance-valid")]
    public async Task<ActionResult<InsuranceValidityResponse>> IsInsuranceValid(long carId, [FromQuery] string date)
    {
        if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", out var parsed))
            return BadRequest("Invalid date format. Use YYYY-MM-DD.");

        try
        {
            var valid = await _service.IsInsuranceValidAsync(carId, parsed);
            return Ok(new InsuranceValidityResponse(carId, parsed.ToString("yyyy-MM-dd"), valid));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
    
    [HttpPost("cars")]
    public async Task<ActionResult<CarDto>> CreateCar([FromBody] CreateCarDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        if (await _service.IsCarAlreadyRegistered(dto.Vin))
            return Conflict($"Car with VIN {dto.Vin} is already registered.");

        try
        {
            var carId = await _service.CreateCarAsync(dto);
            return CreatedAtAction(nameof(GetCars), new { id = carId }, dto);
        }
        catch (KeyNotFoundException)
        {
            return NotFound($"Owner with ID {dto.OwnerId} not found.");
        }
    }
    
    // B: Register a claim
    [HttpPost("cars/{carId:long}/claims")]
    public async Task<ActionResult<ClaimDto>> RegisterClaim(long carId, [FromBody] CreateClaimDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var claim = await _service.RegisterClaimAsync(carId, dto);
            return CreatedAtAction(nameof(GetCarHistory), new { carId }, claim);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    // B: Get car history
    [HttpGet("cars/{carId:long}/history")]
    public async Task<ActionResult<CarHistoryDto>> GetCarHistory(long carId)
    {
        try
        {
            var history = await _service.GetCarHistoryAsync(carId);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }
}
