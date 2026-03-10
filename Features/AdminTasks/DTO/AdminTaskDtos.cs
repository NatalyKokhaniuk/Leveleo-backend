using LeveLEO.Features.AdminTasks.Models;

namespace LeveLEO.Features.AdminTasks.DTO;

public class AdminTaskResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public AdminTaskType Type { get; set; }
    public string TypeDisplay { get; set; } = null!;
    public AdminTaskPriority Priority { get; set; }
    public string PriorityDisplay { get; set; } = null!;
    public AdminTaskStatus Status { get; set; }
    public string StatusDisplay { get; set; } = null!;
    public Guid? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? Metadata { get; set; }
    public string? AssignedTo { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public string? CompletionNote { get; set; }
}

public class CompleteTaskDto
{
    public string? CompletionNote { get; set; }
}

public class AdminTaskFilterDto
{
    public AdminTaskStatus? Status { get; set; }
    public AdminTaskType? Type { get; set; }
    public AdminTaskPriority? Priority { get; set; }
    public string? AssignedTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
