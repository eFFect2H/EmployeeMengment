namespace BankEmployeeManagement.DTOs
{
    public class EmployeeHistoryDTO
    {
        public int Id { get; set; }
        public int EmployeeId { get; set; }
        public string Action { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Details { get; set; }
    }
}
