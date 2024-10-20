namespace GoParkAPI.DTO
{
    public class ReservationDTO
    {
        public int resId { get; set; }

        public DateTime resTime { get; set; }

        public string lotName { get; set; } = null!;

        public string licensePlate { get; set; } = null!;

        public bool isCanceled { get; set; }

        public bool isOverdue { get; set; }

        public bool isFinish { get; set; }

        public decimal? latitude { get; set; }  //緯度

        public decimal? longitude { get; set; }  //經度

    }




}