using LeveLEO.Features.AdminTasks.DTO;
using LeveLEO.Features.AdminTasks.Services;
using LeveLEO.Features.Products.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LeveLEO.Features.AdminTasks.Controllers;

[ApiController]
[Route("api/admin/[controller]")]
[Authorize(Roles = "Admin,Moderator")]
public class TasksController(IAdminTaskService taskService) : ControllerBase
{
    /// <summary>
    /// Отримати список завдань з фільтрацією
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResultDto<AdminTaskResponseDto>>> GetTasks([FromQuery] AdminTaskFilterDto filter)
    {
        var result = await taskService.GetTasksAsync(filter);
        return Ok(result);
    }

    /// <summary>
    /// Отримати завдання за ID
    /// </summary>
    [HttpGet("{taskId:guid}")]
    public async Task<ActionResult<AdminTaskResponseDto>> GetTaskById(Guid taskId)
    {
        var task = await taskService.GetTaskByIdAsync(taskId);
        return Ok(task);
    }

    /// <summary>
    /// Призначити завдання собі
    /// </summary>
    [HttpPost("{taskId:guid}/assign")]
    public async Task<ActionResult<AdminTaskResponseDto>> AssignToMe(Guid taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var task = await taskService.AssignTaskAsync(taskId, userId);
        return Ok(task);
    }

    /// <summary>
    /// Завершити завдання
    /// </summary>
    [HttpPost("{taskId:guid}/complete")]
    public async Task<ActionResult<AdminTaskResponseDto>> CompleteTask(Guid taskId, [FromBody] CompleteTaskDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var task = await taskService.CompleteTaskAsync(taskId, userId, dto);
        return Ok(task);
    }

    /// <summary>
    /// Скасувати завдання
    /// </summary>
    [HttpPost("{taskId:guid}/cancel")]
    public async Task<ActionResult<AdminTaskResponseDto>> CancelTask(Guid taskId)
    {
        var task = await taskService.CancelTaskAsync(taskId);
        return Ok(task);
    }
}
