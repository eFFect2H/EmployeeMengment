namespace BankEmployeeManagement.Models
{
    public class Branch
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Address { get; set; }
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
