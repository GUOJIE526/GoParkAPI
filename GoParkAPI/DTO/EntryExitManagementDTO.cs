namespace GoParkAPI.DTO
{
    public class EntryExitManagementDTO
    {
        public int entryexitId { get; set; }

        public string lotName { get; set; } = null!;

        public string licensePlate { get; set; } = null!;

        public DateTime entryTime { get; set; }

        public DateTime? exitTime { get; set; }

        public int totalMins { get; set; }

        public int amount { get; set; }

        
    }
}