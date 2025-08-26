namespace CarInsurance.Api.Models;

public class Claim
{
    public long Id { get; set; }

    public long CarId { get; set; }
    public Car Car { get; set; } = null!;

    public DateOnly ClaimDate { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}