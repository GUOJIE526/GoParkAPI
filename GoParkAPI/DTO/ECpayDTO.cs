namespace GoParkAPI.DTO
{
    public class ECpayDTO
    {
        public string? ItemName { get; set; }    // 商品名稱
        public string? PlanName { get; set; } //方案名稱
        public int? TotalAmount { get; set; } // 交易金額
        public string ClientBackURL { get; set; }
    }
}
