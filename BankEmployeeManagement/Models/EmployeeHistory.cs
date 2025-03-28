namespace BankEmployeeManagement.Models
{
    public class EmployeeHistory
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string? Action { get; set; } // "Created", "Updated", "Deleted"
        public string? ChangedBy { get; set; } // Имя пользователя из токена
        public DateTime ChangedAt { get; set; }
        public string? Details { get; set; } // JSON с изменёнными данными
    }
}
