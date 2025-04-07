namespace BankEmployeeManagement.Models
{
    public class Taske
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime Deadline { get; set; }
        public int Priority { get; set; } // 1 - Низкий, 2 - Средний, 3 - Высокий
        public string Status { get; set; } // "InProgress", "Completed", "Overdue"
        public int AssignedEmployeeId { get; set; }
        public Employee AssignedEmployee { get; set; }
        public int BranchId { get; set; }
        public Branch Branch { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } // Имя пользователя, создавшего задачу
    }
}
