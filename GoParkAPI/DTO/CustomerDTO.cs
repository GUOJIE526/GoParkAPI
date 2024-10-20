namespace GoParkAPI.DTO
{
    public class CustomerDTO
    {
        public int UserId { get; set; }
        public string Username { get; set; } = null!;
        public string Password { get; set; }
        public string Salt { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string LicensePlate { get; set; } = null!;

    }
}