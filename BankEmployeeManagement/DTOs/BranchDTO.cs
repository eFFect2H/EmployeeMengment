namespace BankEmployeeManagement.DTOs
{
    public class BranchDTO
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
    }

    public class BranchImportDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
    }
}
