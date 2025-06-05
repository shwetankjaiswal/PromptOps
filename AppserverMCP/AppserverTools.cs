using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;

namespace AppserverMCP;

[McpServerToolType]
public sealed class AppserverTools(AppserverService appserverService)
{
    private AppserverService? _appserverService = appserverService;

    [McpServerTool, Description("Get comprehensive information about the Appserver including version and all available models with their status")]
    public async Task<string> GetAppserverAbout()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var aboutInfo = await _appserverService.GetAboutAsync();
            if (aboutInfo == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve Appserver information. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(aboutInfo, AppserverContext.Default.AboutOutputView);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving Appserver information: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Execute a task by ID")]
    public async Task<string> ExecuteTask(
        [Description("ID of the task to execute")] string task_id,
        [Description("Optional reason for task execution")] string? reason = null)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(task_id))
            return JsonSerializer.Serialize(new { error = "Task ID is required" });

        try
        {
            var executionResult = await _appserverService.ExecuteTaskAsync(task_id, reason);
            if (executionResult == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to execute task. The server may be unavailable or the task ID may be invalid." });
            }

            return JsonSerializer.Serialize(executionResult, AppserverContext.Default.TaskExecutionResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error executing task: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get current status and details of a task")]
    public async Task<string> GetTaskStatus(
        [Description("ID of the task to check")] string task_id)
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        if (string.IsNullOrWhiteSpace(task_id))
            return JsonSerializer.Serialize(new { error = "Task ID is required" });

        try
        {
            var statusResult = await _appserverService.GetTaskStatusAsync(task_id);
            if (statusResult == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve task status. The server may be unavailable or the task ID may be invalid." });
            }

            return JsonSerializer.Serialize(statusResult, AppserverContext.Default.TaskStatusResponse);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving task status: {ex.Message}" });
        }
    }

    [McpServerTool, Description("Get list of all tasks from the Appserver")]
    public async Task<string> GetTasks()
    {
        if (_appserverService == null)
            return JsonSerializer.Serialize(new { error = "AppserverService not initialized" });

        try
        {
            var tasksList = await _appserverService.GetTasksAsync();
            if (tasksList == null)
            {
                return JsonSerializer.Serialize(new { error = "Failed to retrieve tasks list. The server may be unavailable." });
            }

            return JsonSerializer.Serialize(tasksList, typeof(List<TaskItemView>), AppserverContext.Default);
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { error = $"Error retrieving tasks list: {ex.Message}" });
        }
    }
} 