using LeveLEO.Data;
using LeveLEO.Features.AdminTasks.DTO;
using LeveLEO.Features.AdminTasks.Models;
using LeveLEO.Features.Products.DTO;
using Microsoft.EntityFrameworkCore;

namespace LeveLEO.Features.AdminTasks.Services;

public interface IAdminTaskService
{
    Task<AdminTaskResponseDto> CreateTaskAsync(AdminTask task);
    Task<PagedResultDto<AdminTaskResponseDto>> GetTasksAsync(AdminTaskFilterDto filter);
    Task<AdminTaskResponseDto> GetTaskByIdAsync(Guid taskId);
    Task<AdminTaskResponseDto> AssignTaskAsync(Guid taskId, string userId);
    Task<AdminTaskResponseDto> CompleteTaskAsync(Guid taskId, string userId, CompleteTaskDto dto);
    Task<AdminTaskResponseDto> CancelTaskAsync(Guid taskId);
}

public class AdminTaskService(AppDbContext db) : IAdminTaskService
{
    public async Task<AdminTaskResponseDto> CreateTaskAsync(AdminTask task)
    {
        db.AdminTasks.Add(task);
        await db.SaveChangesAsync();
        return MapToDto(task);
    }

    public async Task<PagedResultDto<AdminTaskResponseDto>> GetTasksAsync(AdminTaskFilterDto filter)
    {
        var query = db.AdminTasks.AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);

        if (filter.Type.HasValue)
            query = query.Where(t => t.Type == filter.Type.Value);

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (!string.IsNullOrEmpty(filter.AssignedTo))
            query = query.Where(t => t.AssignedTo == filter.AssignedTo);

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResultDto<AdminTaskResponseDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<AdminTaskResponseDto> GetTaskByIdAsync(Guid taskId)
    {
        var task = await db.AdminTasks.FindAsync(taskId)
            ?? throw new ApiException("TASK_NOT_FOUND", "Admin task not found.", 404);

        return MapToDto(task);
    }

    public async Task<AdminTaskResponseDto> AssignTaskAsync(Guid taskId, string userId)
    {
        var task = await db.AdminTasks.FindAsync(taskId)
            ?? throw new ApiException("TASK_NOT_FOUND", "Admin task not found.", 404);

        task.AssignedTo = userId;
        task.Status = AdminTaskStatus.InProgress;

        await db.SaveChangesAsync();
        return MapToDto(task);
    }

    public async Task<AdminTaskResponseDto> CompleteTaskAsync(Guid taskId, string userId, CompleteTaskDto dto)
    {
        var task = await db.AdminTasks.FindAsync(taskId)
            ?? throw new ApiException("TASK_NOT_FOUND", "Admin task not found.", 404);

        if (task.Status == AdminTaskStatus.Completed)
            throw new ApiException("TASK_ALREADY_COMPLETED", "This task is already completed.", 400);

        task.Status = AdminTaskStatus.Completed;
        task.CompletedAt = DateTimeOffset.UtcNow;
        task.CompletionNote = dto.CompletionNote;

        if (string.IsNullOrEmpty(task.AssignedTo))
            task.AssignedTo = userId;

        await db.SaveChangesAsync();
        return MapToDto(task);
    }

    public async Task<AdminTaskResponseDto> CancelTaskAsync(Guid taskId)
    {
        var task = await db.AdminTasks.FindAsync(taskId)
            ?? throw new ApiException("TASK_NOT_FOUND", "Admin task not found.", 404);

        task.Status = AdminTaskStatus.Cancelled;
        await db.SaveChangesAsync();
        return MapToDto(task);
    }

    private static AdminTaskResponseDto MapToDto(AdminTask task)
    {
        return new AdminTaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            TypeDisplay = task.Type.ToString(),
            Priority = task.Priority,
            PriorityDisplay = task.Priority.ToString(),
            Status = task.Status,
            StatusDisplay = task.Status.ToString(),
            RelatedEntityId = task.RelatedEntityId,
            RelatedEntityType = task.RelatedEntityType,
            Metadata = task.Metadata,
            AssignedTo = task.AssignedTo,
            CreatedAt = task.CreatedAt,
            CompletedAt = task.CompletedAt,
            CompletionNote = task.CompletionNote
        };
    }
}
