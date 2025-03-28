namespace BankEmployeeManagement.DTOs
{
    public class UserRegisterDTO
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
        public int RoleId { get; set; } // Роль пользователя
    }
}
