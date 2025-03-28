using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using BankEmployeeManagement.Data;
using System.Text.Json;
using System.Formats.Asn1;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace BankEmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BranchController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("CreateBranch")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> CreateBranch([FromBody] BranchDTO branchDto)
        {
            var branch = new Branch
            {
                Name = branchDto.Name,
                Address = branchDto.Address
            };

            _context.Branches.Add(branch);
            await _context.SaveChangesAsync();

            // Логирование создания
            var history = new BranchHistory
            {
                BranchId = branch.Id,
                Action = "Created",
                ChangedBy = User.Identity.Name ?? "Unknown",
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(branchDto)
            };
            _context.BranchHistory.Add(history);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBranch), new { id = branch.Id }, branch);
        }

        [HttpGet("IndexBranch")]
        public async Task<ActionResult<IEnumerable<BranchDTO>>> IndexBranch()
        {
            return await _context.Branches
                .Select(b => new BranchDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address
                })
                .ToListAsync();
        }

        [HttpGet("GetBranch/{id}")]
        public async Task<ActionResult<BranchDTO>> GetBranch(int id)
        {
            var branch = await _context.Branches
                .Where(b => b.Id == id)
                .Select(b => new BranchDTO
                {
                    Id = b.Id,
                    Name = b.Name,
                    Address = b.Address
                })
                .FirstOrDefaultAsync();

            if (branch == null) return NotFound();
            return branch;
        }

        [HttpPut("EditBranch/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> EditBranch(int id, [FromBody] BranchDTO branchDto)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            var oldData = new BranchDTO
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address
            };

            branch.Name = branchDto.Name;
            branch.Address = branchDto.Address;

            await _context.SaveChangesAsync();

            // Логирование изменения
            var history = new BranchHistory
            {
                BranchId = id,
                Action = "Updated",
                ChangedBy = User.Identity.Name ?? "Unknown",
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(new { Old = oldData, New = branchDto })
            };
            _context.BranchHistory.Add(history);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("DeleteBranch/{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteBranch(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound();

            var branchDto = new BranchDTO
            {
                Id = branch.Id,
                Name = branch.Name,
                Address = branch.Address
            };

            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();

            // Логирование удаления
            var history = new BranchHistory
            {
                BranchId = id,
                Action = "Deleted",
                ChangedBy = User.Identity.Name ?? "Unknown",
                ChangedAt = DateTime.UtcNow,
                Details = JsonSerializer.Serialize(branchDto)
            };
            _context.BranchHistory.Add(history);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpGet("History/{branchId}")]
        [Authorize(Roles = "Administrator")]
        public async Task<ActionResult<IEnumerable<BranchHistoryDTO>>> GetBranchHistory(int branchId)
        {
            var history = await _context.BranchHistory
                .Where(h => h.BranchId == branchId)
                .Select(h => new BranchHistoryDTO
                {
                    Id = h.Id,
                    BranchId = h.BranchId,
                    Action = h.Action,
                    ChangedBy = h.ChangedBy,
                    ChangedAt = h.ChangedAt,
                    Details = h.Details
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPost("Import")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> ImportBranches(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("Файл не загружен.");

            if (!file.FileName.EndsWith(".csv"))
                return BadRequest("Требуется файл в формате CSV.");

            try
            {
                using var stream = new StreamReader(file.OpenReadStream());
                using var csv = new CsvReader(stream, new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    Delimiter = ";" // Точка с запятой как разделитель
                });
                var branchesFromCsv = csv.GetRecords<BranchImportDTO>().ToList();

                var addedBranches = new List<(Branch Branch, BranchImportDTO Dto)>();
                var histories = new List<BranchHistory>();

                foreach (var branchDto in branchesFromCsv)
                {
                    var branch = new Branch
                    {
                        Name = branchDto.Name,
                        Address = branchDto.Address
                    };
                    addedBranches.Add((branch, branchDto));
                    _context.Branches.Add(branch);

                    var history = new BranchHistory
                    {
                        BranchId = 0,
                        Action = "Created",
                        ChangedBy = User.Identity.Name ?? "Unknown",
                        ChangedAt = DateTime.UtcNow,
                        Details = JsonSerializer.Serialize(branchDto)
                    };
                    histories.Add(history);
                    _context.BranchHistory.Add(history);
                }

                await _context.SaveChangesAsync();

                for (int i = 0; i < addedBranches.Count; i++)
                {
                    histories[i].BranchId = addedBranches[i].Branch.Id;
                }
                await _context.SaveChangesAsync();

                return Ok(new { Message = $"Успешно импортировано {branchesFromCsv.Count} филиалов." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Ошибка при импорте: {ex.Message}");
            }
        }
    }
}
