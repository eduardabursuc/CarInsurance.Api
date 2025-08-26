namespace CarInsurance.Api.Dtos;

public record CarDto(long Id, string Vin, string? Make, string? Model, int Year, long OwnerId, string OwnerName, string? OwnerEmail);
public record CreateCarDto(string Vin, string? Make, string? Model, int Year, long OwnerId);

public record InsuranceValidityResponse(long CarId, string Date, bool Valid);

public record CreateClaimDto(DateOnly ClaimDate, string Description, decimal Amount);
public record ClaimDto(long Id, DateOnly ClaimDate, string Description, decimal Amount);

public record PolicyPeriodDto(long Id, DateOnly StartDate, DateOnly EndDate, string Provider);

public record CarHistoryDto(
    long CarId,
    List<PolicyPeriodDto> Policies,
    List<ClaimDto> Claims
);