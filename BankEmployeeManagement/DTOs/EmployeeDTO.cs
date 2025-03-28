namespace BankEmployeeManagement.DTOs
{
    public class EmployeeDTO
    {
        public int EmployeeId { get; set; }
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public int? BranchId { get; set; }
        public decimal? Salary { get; set; }
    }
}
