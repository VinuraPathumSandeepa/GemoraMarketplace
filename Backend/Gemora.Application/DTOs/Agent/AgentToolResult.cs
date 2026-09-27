namespace Gemora.Application.DTOs.Agent;

public class AgentToolResult<T>
{
    public bool Success { get; set; }

    public string? ErrorCode { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}
