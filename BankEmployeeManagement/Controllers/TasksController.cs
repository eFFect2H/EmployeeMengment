using BankEmployeeManagement.DTOs;
using BankEmployeeManagement.Models;
using BankEmployeeManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankEmployeeManagement.Controllers
{
    [Authorize(Roles = "Administrator,HeadBranch")]
    [Route("api/[controller]")]
    [ApiController]
    public class TasksController : ControllerBase
    {
        private readonly ITaskService _taskService;

        public TasksController(ITaskService taskService)
        {
            _taskService = taskService;
        }

        [HttpPost("CreateTask")]
        public async Task<IActionResult> CreateTask([FromBody] TaskCreateDTO taskDto)
        {
            var username = User.Identity.Name;
            var (success, message, task) = await _taskService.CreateTaskAsync(taskDto, username);
            if (!success) return BadRequest(new { message });
            return Ok(task);
        }

        [HttpDelete("DeleteTask/{taskId}")]
        public async Task<IActionResult> DeleteTask(int taskId)
        {
            var success = await _taskService.DeleteTaskAsync(taskId);
            if (!success) return NotFound(new { message = "Задача не найдена" });
            return Ok(new { message = "Задача удалена" });
        }

        [HttpPut("UpdateTask/{taskId}")]
        public async Task<IActionResult> UpdateTask(int taskId, [FromBody] TaskUpdateDTO taskDto)
        {
            var task = await _taskService.UpdateTaskAsync(taskId, taskDto);
            if (task == null) return NotFound(new { message = "Задача не найдена" });
            return Ok(task);
        }

        [HttpGet("GetTasksByBranch/{branchId}")]
        public async Task<IActionResult> GetTasksByBranch(int branchId)
        {
            var tasks = await _taskService.GetTasksByBranchAsync(branchId);
            return Ok(tasks);
        }

        [HttpGet("GetAllTasks")]
        public async Task<IActionResult> GetAllTasks()
        {
            var tasks = await _taskService.GetAllTasksAsync();
            return Ok(tasks);
        }
    }
}
