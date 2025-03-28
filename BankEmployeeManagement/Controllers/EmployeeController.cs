using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using BankEmployeeManagement.Data;
using System.Text.Json;

namespace BankEmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;

        public EmployeeController(AppDbContext context)
        {
            _context = context;
        }

        // POST: /api/Employee
        [HttpPost("CreateEmployee")]
        [Authorize(Roles = "Administrator,HeadBranch")]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeDTO employeeDto)
        {
            if (!await _context.Branches.AnyAsync(b => b.Id == employeeDto.BranchId))
                return BadRequest("Указанный филиал не существует.");

            var employee = new Employee
            {
                FullName = employeeDto.FullName,
                Phone = employeeDto.Phone,
                Email = employeeDto.Email,
                Position = employeeDto.Position,
                BranchId = employeeDto.BranchId,
                Salary = employeeDto.Salary
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            // Логирование создания
            var history = new EmployeeHistory
            {
                EmployeeId = employee.EmployeeId,
                Action = "Created",
                ChangedBy = User.Identity.Name ?? "Unknown", // Получаем имя из токена
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(employeeDto)
            };
            _context.EmployeeHistory.Add(history);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, employee);
        }

        // GET: /api/Employee
        [HttpGet("IndexEmployee")]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> IndexEmployee()
        {
            return await _context.Employees
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    Phone = e.Phone,
                    Email = e.Email,
                    Position = e.Position,
                    BranchId = e.BranchId,
                    Salary = e.Salary
                })
                .ToListAsync();
        }

        // GET: /api/Employee/{id}
        [HttpGet("GetEmployee/{id}")]
        public async Task<ActionResult<EmployeeDTO>> GetEmployee(int id)
        {
            var employee = await _context.Employees
                .Where(e => e.EmployeeId == id)
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    Phone = e.Phone,
                    Email = e.Email,
                    Position = e.Position,
                    BranchId = e.BranchId,
                    Salary = e.Salary
                })
                .FirstOrDefaultAsync();

            if (employee == null) return NotFound();

            return employee;
        }

        // PUT: /api/Employee/{id}
        [HttpPut("EditEmployee/{id}")]
        [Authorize(Roles = "Administrator,HeadBranch")]
        public async Task<IActionResult> EditEmployee(int id, [FromBody] EmployeeDTO employeeDto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            // Сохраняем старые данные для истории
            var oldData = new EmployeeDTO
            {
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                Position = employee.Position,
                BranchId = employee.BranchId,
                Salary = employee.Salary
            };

            employee.FullName = employeeDto.FullName;
            employee.Phone = employeeDto.Phone;
            employee.Email = employeeDto.Email;
            employee.Position = employeeDto.Position;
            employee.BranchId = employeeDto.BranchId;
            employee.Salary = employeeDto.Salary;


            // Логирование изменения
            var history = new EmployeeHistory
            {
                EmployeeId = id,
                Action = "Updated",
                ChangedBy = User.Identity.Name ?? "Unknown",
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new { Old = oldData, New = employeeDto })
            };
            _context.EmployeeHistory.Add(history);

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/Employee/{id}
        [HttpDelete("DeleteEmployee/{id}")]
        [Authorize(Roles = "Administrator,HeadBranch")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null) return NotFound();

            var employeeDto = new EmployeeDTO
            {
                FullName = employee.FullName,
                Phone = employee.Phone,
                Email = employee.Email,
                Position = employee.Position,
                BranchId = employee.BranchId,
                Salary = employee.Salary
            };

            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync();

            // Логирование удаления
            var history = new EmployeeHistory
            {
                EmployeeId = id,
                Action = "Deleted",
                ChangedBy = User.Identity.Name ?? "Unknown",
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(employeeDto)
            };
            _context.EmployeeHistory.Add(history);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("History/{employeeId}")]
        [Authorize(Roles = "Administrator,HeadBranch")]
        public async Task<ActionResult<IEnumerable<EmployeeHistoryDTO>>> GetEmployeeHistory(int employeeId)
        {
            var history = await _context.EmployeeHistory
                .Where(h => h.EmployeeId == employeeId)
                .Select(h => new EmployeeHistoryDTO
                {
                    Id = h.Id,
                    EmployeeId = h.EmployeeId,
                    Action = h.Action,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Details = h.Details
                })
                .ToListAsync();

            return Ok(history);
        }
        // ------------------------
        [HttpGet("SearchEmployees")]
        [Authorize(Roles = "Administrator,HeadBranch")]
        public async Task<ActionResult<IEnumerable<EmployeeDTO>>> SearchEmployees(
            [FromQuery] string? fullName,
            [FromQuery] int? branchId,
            [FromQuery] string? position,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var query = _context.Employees.AsQueryable();

            if (!string.IsNullOrEmpty(fullName))
                query = query.Where(e => e.FullName.Contains(fullName));
            if (branchId.HasValue)
                query = query.Where(e => e.BranchId == branchId);
            if (!string.IsNullOrEmpty(position))
                query = query.Where(e => e.Position == position);

            var totalItems = await query.CountAsync();
            var employees = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new EmployeeDTO
                {
                    EmployeeId = e.EmployeeId,
                    FullName = e.FullName,
                    Phone = e.Phone,
                    Email = e.Email,
                    Position = e.Position,
                    BranchId = e.BranchId,
                    Salary = e.Salary
                })
                .ToListAsync();

            return Ok(new
            {
                TotalItems = totalItems,
                Page = page,
                PageSize = pageSize,
                Employees = employees
            });
        }
    }
}
