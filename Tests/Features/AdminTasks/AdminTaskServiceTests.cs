using LeveLEO.Data;
using LeveLEO.Features.AdminTasks.DTO;
using LeveLEO.Features.AdminTasks.Models;
using LeveLEO.Features.AdminTasks.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LeveLEO.Tests.Features.AdminTasks;

public class AdminTaskServiceTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly AdminTaskService _service;

    public AdminTaskServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _service = new AdminTaskService(_context);
    }

    [Fact]
    public async Task CreateTaskAsync_ShouldCreateTask()
    {
        var task = new AdminTask
        {
            Title = "Test Task",
            Description = "Test Description",
            Type = AdminTaskType.ModerateReview,
            Priority = AdminTaskPriority.Normal
        };

        var result = await _service.CreateTaskAsync(task);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal("Test Task", result.Title);
        Assert.Equal(AdminTaskStatus.Pending, result.Status);
    }

    [Fact]
    public async Task GetTasksAsync_WithStatusFilter_ShouldReturnFiltered()
    {
        await _service.CreateTaskAsync(new AdminTask
        {
            Title = "Pending Task",
            Description = "Desc",
            Type = AdminTaskType.ShipOrder,
            Priority = AdminTaskPriority.High,
            Status = AdminTaskStatus.Pending
        });

        await _service.CreateTaskAsync(new AdminTask
        {
            Title = "Completed Task",
            Description = "Desc",
            Type = AdminTaskType.ShipOrder,
            Priority = AdminTaskPriority.High,
            Status = AdminTaskStatus.Completed
        });

        var filter = new AdminTaskFilterDto
        {
            Status = AdminTaskStatus.Pending,
            Page = 1,
            PageSize = 10
        };

        var result = await _service.GetTasksAsync(filter);

        Assert.Equal(1, result.TotalCount);
        Assert.All(result.Items, task => Assert.Equal(AdminTaskStatus.Pending, task.Status));
    }

    [Fact]
    public async Task AssignTaskAsync_ShouldAssignToUser()
    {
        var task = await _service.CreateTaskAsync(new AdminTask
        {
            Title = "Unassigned Task",
            Description = "Desc",
            Type = AdminTaskType.ModerateReview,
            Priority = AdminTaskPriority.Normal
        });

        var userId = "user-123";

        var result = await _service.AssignTaskAsync(task.Id, userId);

        Assert.Equal(userId, result.AssignedTo);
        Assert.Equal(AdminTaskStatus.InProgress, result.Status);
    }

    [Fact]
    public async Task CompleteTaskAsync_ShouldMarkAsCompleted()
    {
        var task = await _service.CreateTaskAsync(new AdminTask
        {
            Title = "Task to Complete",
            Description = "Desc",
            Type = AdminTaskType.ModerateReview,
            Priority = AdminTaskPriority.Normal
        });

        var userId = "user-123";
        var completeDto = new CompleteTaskDto
        {
            CompletionNote = "All done!"
        };

        var result = await _service.CompleteTaskAsync(task.Id, userId, completeDto);

        Assert.Equal(AdminTaskStatus.Completed, result.Status);
        Assert.Equal("All done!", result.CompletionNote);
        Assert.NotNull(result.CompletedAt);
    }

    [Fact]
    public async Task CompleteTaskAsync_AlreadyCompleted_ShouldThrowException()
    {
        var task = await _service.CreateTaskAsync(new AdminTask
        {
            Title = "Task",
            Description = "Desc",
            Type = AdminTaskType.ModerateReview,
            Priority = AdminTaskPriority.Normal,
            Status = AdminTaskStatus.Completed
        });

        var completeDto = new CompleteTaskDto();

        await Assert.ThrowsAsync<ApiException>(async () =>
            await _service.CompleteTaskAsync(task.Id, "user", completeDto)
        );
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
