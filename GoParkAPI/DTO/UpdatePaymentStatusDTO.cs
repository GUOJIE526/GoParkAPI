namespace GoParkAPI.DTO
{
    public class UpdatePaymentStatusDTO
    {
        public string OrderId { get; set; } // 從前端傳來的訂單 ID
        public int? UserId {  get; set; }

    }
}
