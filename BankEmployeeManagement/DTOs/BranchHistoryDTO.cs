﻿namespace BankEmployeeManagement.DTOs
{
    public class BranchHistoryDTO
    {
        public int Id { get; set; }
        public int BranchId { get; set; }
        public string Action { get; set; }
        public string ChangedBy { get; set; }
        public DateTime ChangedAt { get; set; }
        public string Details { get; set; }
    }
}
