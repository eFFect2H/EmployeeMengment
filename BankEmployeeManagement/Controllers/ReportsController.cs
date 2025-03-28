using BankEmployeeManagement.Data;
using BankEmployeeManagement.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankEmployeeManagement.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Administrator,HeadBranch")]
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Отчет по количеству сотрудников в каждом филиале.
        /// </summary>
        [HttpGet("BranchEmployeeCount")]
        public async Task<ActionResult<IEnumerable<BranchEmployeeCountReportDTO>>> GetBranchEmployeeCountReport()
        {
            var report = await _context.Branches
                .GroupJoin(
                    _context.Employees,
                    branch => branch.Id,
                    employee => employee.BranchId,
                    (branch, employees) => new BranchEmployeeCountReportDTO
                    {
                        BranchId = branch.Id,
                        BranchName = branch.Name,
                        EmployeeCount = employees.Count()
                    })
                .OrderBy(r => r.BranchName)
                .ToListAsync();

            return Ok(report);
        }

        /// <summary>
        /// Отчет по зарплатам сотрудников.
        /// </summary>
        /// <param name="branchId">Фильтр по филиалу (опционально).</param>
        [HttpGet("SalaryReport")]
        public async Task<ActionResult<SalaryReportDTO>> GetSalaryReport([FromQuery] int? branchId)
        {
            var query = _context.Employees.AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(e => e.BranchId == branchId);
            }

            var salaryStats = await query
                .Where(e => e.Salary.HasValue)
                .GroupBy(e => 1)
                .Select(g => new SalaryReportDTO
                {
                    TotalSalary = g.Sum(e => e.Salary.Value),
                    AverageSalary = g.Average(e => e.Salary.Value),
                    MinSalary = g.Min(e => e.Salary.Value),
                    MaxSalary = g.Max(e => e.Salary.Value),
                    EmployeeCount = g.Count()
                })
                .FirstOrDefaultAsync();

            if (salaryStats == null || salaryStats.EmployeeCount == 0)
            {
                return Ok(new SalaryReportDTO
                {
                    TotalSalary = 0,
                    AverageSalary = 0,
                    MinSalary = 0,
                    MaxSalary = 0,
                    EmployeeCount = 0
                });
            }

            return Ok(salaryStats);
        }

        /// <summary>
        /// Отчет по распределению сотрудников по должностям.
        /// </summary>
        /// <param name="branchId">Фильтр по филиалу (опционально).</param>
        [HttpGet("PositionDistribution")]
        public async Task<ActionResult<IEnumerable<PositionDistributionReportDTO>>> GetPositionDistributionReport([FromQuery] int? branchId)
        {
            var query = _context.Employees
                .Where(e => !string.IsNullOrEmpty(e.Position))
                .AsQueryable();

            if (branchId.HasValue)
            {
                query = query.Where(e => e.BranchId == branchId);
            }

            var report = await query
                .GroupBy(e => e.Position)
                .Select(g => new PositionDistributionReportDTO
                {
                    Position = g.Key,
                    EmployeeCount = g.Count()
                })
                .OrderByDescending(r => r.EmployeeCount)
                .ToListAsync();

            return Ok(report);
        }
    }
}
