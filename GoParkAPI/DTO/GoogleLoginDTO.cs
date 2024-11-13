namespace GoParkAPI.DTO
{
    public class GoogleLoginDTO
    {
        public string Token { get; set; } // Google OAuth Token
        public string LicensePlate { get; set; } // 用戶輸入的車牌
    }

    public class MergeAccountsDTO
    {
        public string GoogleToken { get; set; }
        public int UserId { get; set; }
    }

    public class GoogleUserDTO
    {
        public string Email { get; set; }
        public string Name { get; set; }
    }
}