namespace GoParkAPI.DTO
{
    internal class ParkingLotDTO
    {
        public string LotName { get; set; } = null!;
        public decimal TWD97Y { get; set; }  // 緯度
        public decimal TWD97X { get; set; }  // 經度

        public string Location { get; set; } = null!;

        public int LargeCarSpaces { get; set; }

        public int SmallCarSpaces { get; set; }

        public int MotorcycleSpaces { get; set; }

        public int ChildFriendlySpaces { get; set; }

        public int MotherSpace { get; set; }

        public int HourlyRate { get; set; }

        public int OpeningHours { get; set; }

        public string? Telephone { get; set; }

        public int VaildSpace { get; set; }

    }
}