namespace BankEmployeeManagement.DTOs
{
    public class BranchEmployeeCountReportDTO
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public int EmployeeCount { get; set; }
    }

    public class SalaryReportDTO
    {
        public decimal TotalSalary { get; set; }
        public decimal AverageSalary { get; set; }
        public decimal MinSalary { get; set; }
        public decimal MaxSalary { get; set; }
        public int EmployeeCount { get; set; }
    }

    public class PositionDistributionReportDTO
    {
        public string Position { get; set; } = null!;
        public int EmployeeCount { get; set; }
    }
}
