using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using BankEmployeeManagement.Data;
using System.Text.Json;
using CsvHelper.Configuration;
using CsvHelper;
using System.Globalization;
using Microsoft.Extensions.Caching.Memory;

namespace BankEmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EmployeeController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IMemoryCache _cache;
        private const string EmployeesListCacheKey = "EmployeesList";

        public EmployeeController(AppDbContext context, IMemoryCache cache)
        {
            _context = context;
            _cache = cache;
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
            // Проверяем кэш
            if (_cache.TryGetValue(EmployeesListCacheKey, out List<EmployeeDTO> cachedEmployees))
            {
                return Ok(cachedEmployees);
            }

            var employees = await _context.Employees
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

            // Настраиваем параметры кэша
            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(20), 
                SlidingExpiration = TimeSpan.FromMinutes(10) 
            };

            
            _cache.Set(EmployeesListCacheKey, employees, cacheEntryOptions);

            return Ok(employees);
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

        [HttpPost("Import")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ImportEmployees(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("Please upload a valid CSV file.");
            }

            if (!file.FileName.EndsWith(".csv"))
            {
                return BadRequest("File must be in CSV format.");
            }

            try
            {
                // Настройка конфигурации CSV
                var config = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ",",
                    HasHeaderRecord = true,
                    TrimOptions = TrimOptions.Trim,
                    MissingFieldFound = null
                };

                // Загрузка существующих Id филиалов 
                var branchIdsList = await _context.Branches.Select(b => b.Id).ToListAsync();
                var validBranchIds = new HashSet<int>(branchIdsList);

                using (var streamReader = new StreamReader(file.OpenReadStream()))
                using (var csvReader = new CsvReader(streamReader, config))
                {
                    // Чтение CSV в список EmployeeDTO
                    var employeeDtos = csvReader.GetRecords<EmployeeDTO>().ToList();

                    if (!employeeDtos.Any())
                    {
                        return BadRequest("CSV file is empty.");
                    }

                    // Подготовка списков для сотрудников и истории
                    var employees = new List<Employee>();
                    var histories = new List<EmployeeHistory>();
                    var tempEmployeeMap = new List<(Employee Employee, EmployeeDTO Dto)>(); // Для связи EmployeeId

                    // Валидация и преобразование в Employee
                    foreach (var dto in employeeDtos)
                    {

                        // Базовая валидация
                        if (string.IsNullOrWhiteSpace(dto.FullName))
                        {
                            return BadRequest($"Invalid FullName for employee ID {dto.EmployeeId}");
                        }

                        // Проверка существования филиала
                        if (dto.BranchId.HasValue && !validBranchIds.Contains(dto.BranchId.Value))
                        {
                            return BadRequest($"Invalid BranchId {dto.BranchId} for employee {dto.FullName}");
                        }

                        var employee = new Employee
                        {
                            EmployeeId = dto.EmployeeId,
                            FullName = dto.FullName,
                            Phone = dto.Phone,
                            Email = dto.Email,
                            
                            Position = dto.Position,
                            BranchId = dto.BranchId,
                            Salary = dto.Salary
                        };

                        employees.Add(employee);
                        tempEmployeeMap.Add((employee, dto));
                    }

                    // Проверка на уникальность EmployeeId
                    var existingEmployeeIds = await _context.Employees
                        .Where(e => employees.Select(x => x.EmployeeId).Contains(e.EmployeeId))
                        .Select(e => e.EmployeeId)
                        .ToListAsync();

                    if (existingEmployeeIds.Any())
                    {
                        return BadRequest($"Employees with IDs {string.Join(", ", existingEmployeeIds)} already exist.");
                    }

                    // Добавление сотрудников в контекст
                    _context.Employees.AddRange(employees);
                    await _context.SaveChangesAsync(); // Сохраняем сотрудников, чтобы получить EmployeeId

                    // Создание записей истории
                    foreach (var (employee, dto) in tempEmployeeMap)
                    {
                        var history = new EmployeeHistory
                        {
                            EmployeeId = employee.EmployeeId, 
                            Action = "Created",
                            ChangedBy = User.Identity?.Name ?? "Unknown",
                            ChangedAt = DateTime.UtcNow,
                            Details = JsonSerializer.Serialize(dto, new JsonSerializerOptions { WriteIndented = false })
                        };
                        histories.Add(history);
                    }

                    // Добавление истории
                    _context.EmployeeHistory.AddRange(histories);
                    await _context.SaveChangesAsync();

                    return Ok(new
                    {
                        Message = $"Successfully imported {employees.Count} employees",
                        ImportedCount = employees.Count
                    });
                }
            }
            catch (CsvHelperException ex)
            {
                return BadRequest($"Error parsing CSV file: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
