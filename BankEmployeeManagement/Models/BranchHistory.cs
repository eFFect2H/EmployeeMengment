namespace BankEmployeeManagement.Models
{
    public class BranchHistory
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string Action { get; set; } // "Created", "Updated", "Deleted"
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Details { get; set; } // JSON с данными
    }
}
