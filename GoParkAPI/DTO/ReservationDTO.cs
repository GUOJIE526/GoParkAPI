namespace GoParkAPI.DTO
{
    public class ReservationDTO
    {
        public int resId { get; set; }

        public DateTime? resTime { get; set; }

        public DateTime? StartTime { get; set; }

        public string lotName { get; set; } = null!;

        public string licensePlate { get; set; } = null!;

        public bool isCanceled { get; set; }

        public bool isOverdue { get; set; }

        public bool isFinish { get; set; }

        public decimal? latitude { get; set; }  //緯度

        public decimal? longitude { get; set; }  //經度

        public int? lotId { get; set; }  // 為了要在預定紀錄導入到預定畫面用

        public DateTime? validUntil { get; set; } //用來判斷若現在訂單還沒完成，是否逾期，若未逾期則可取消訂單

    }




}