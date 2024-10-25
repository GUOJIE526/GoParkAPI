namespace GoParkAPI.DTO
{
    public class PaymentValidationDto
    {
        public string PlanId { get; set; }
        public decimal Amount { get; set; }
    }

    public class PaymentValidationDayDto
    {
        public int lotId { get; set; }
        public int carId { get; set; }
        public int Amount { get; set; }
    }
}

