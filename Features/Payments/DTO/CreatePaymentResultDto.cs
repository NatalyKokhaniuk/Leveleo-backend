namespace LeveLEO.Features.Payments.DTO;

public class CreatePaymentResultDto
{
    public string Payload { get; set; } = null!;
    public DateTimeOffset ExpireAt { get; set; }
    public Guid PaymentId { get; set; }
}