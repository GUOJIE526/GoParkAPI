namespace GoParkAPI.DTO
{
    public class LoginsDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        
    }

    public class exitDTO
    {
        public bool exit { get; set; }
        public int UserId { get; set; }
        public string message { get; set; }
        //public string Username { get; set; }
        //public string Email { get; set; }
        //public string Phone { get; set; }
        //public string? Password { get; set; }
        //public string LicensePlate { get; set; }

    }
}
