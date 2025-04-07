using BankEmployeeManagement.Data;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BankEmployeeManagement.Services
{
    public interface ITaskService
    {
        Task<(bool Success, string Message, TaskDTO Task)> CreateTaskAsync(TaskCreateDTO taskDto, string creatorUsername);
        Task<bool> DeleteTaskAsync(int taskId);
        Task<TaskDTO> UpdateTaskAsync(int taskId, TaskUpdateDTO taskDto);
        Task<List<TaskDTO>> GetTasksByBranchAsync(int branchId);
        Task<List<TaskDTO>> GetAllTasksAsync();
    }

    public class TaskService : ITaskService
    {
        private readonly AppDbContext _context;
        

        public TaskService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Message, TaskDTO Task)> CreateTaskAsync(TaskCreateDTO taskDto, string creatorUsername)
        {
            if (!_context.Employees.Any(e => e.EmployeeId == taskDto.AssignedEmployeeId))
                return (false, "Сотрудник не найден", null);
            if (!_context.Branches.Any(b => b.Id == taskDto.BranchId))
                return (false, "Филиал не найден", null);

            var task = new Taske
            {
                Title = taskDto.Title,
                Description = taskDto.Description,
                Deadline = taskDto.Deadline,
                Priority = taskDto.Priority,
                Status = "InProgress",
                AssignedEmployeeId = taskDto.AssignedEmployeeId,
                BranchId = taskDto.BranchId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = creatorUsername
            };

            _context.Tasks.Add(task);
            await _context.SaveChangesAsync();

            return (true, "Задача создана", MapToDto(task));
        }

        public async Task<bool> DeleteTaskAsync(int taskId)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return false;

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<TaskDTO> UpdateTaskAsync(int taskId, TaskUpdateDTO taskDto)
        {
            var task = await _context.Tasks.FindAsync(taskId);
            if (task == null) return null;

            task.Title = taskDto.Title;
            task.Description = taskDto.Description;
            task.Deadline = taskDto.Deadline;
            task.Priority = taskDto.Priority;
            task.Status = DateTime.UtcNow > task.Deadline && task.Status != "Completed" ? "Overdue" : taskDto.Status;

            await _context.SaveChangesAsync();
            return MapToDto(task);
        }

        public async Task<List<TaskDTO>> GetTasksByBranchAsync(int branchId)
        {
            return await _context.Tasks
                .Where(t => t.BranchId == branchId)
                .Select(t => MapToDto(t))
                .ToListAsync();
        }

        public async Task<List<TaskDTO>> GetAllTasksAsync()
        {
            var tasks = await _context.Tasks.ToListAsync();
            foreach (var task in tasks)
            {
                if (DateTime.UtcNow > task.Deadline && task.Status != "Completed")
                {
                    task.Status = "Overdue";
                }
            }
            await _context.SaveChangesAsync();
            return tasks.Select(t => MapToDto(t)).ToList();
        }

        private TaskDTO MapToDto(Taske task)
        {
            var employee = _context.Employees.FirstOrDefault(e => e.EmployeeId == task.AssignedEmployeeId);
            var branch = _context.Branches.FirstOrDefault(b => b.Id == task.BranchId);
            return new TaskDTO
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                Deadline = task.Deadline,
                Priority = task.Priority,
                Status = task.Status,
                AssignedEmployeeId = task.AssignedEmployeeId,
                AssignedEmployeeName = employee?.FullName,
                BranchId = task.BranchId,
                BranchName = branch?.Name,
                CreatedAt = task.CreatedAt,
                CreatedBy = task.CreatedBy
            };
        }
    }
}
