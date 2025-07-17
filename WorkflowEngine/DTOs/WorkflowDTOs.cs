namespace WorkflowEngine.DTOs;

// Request to create a new workflow definition
public class CreateWorkflowDefinitionRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<StateDto> States { get; set; } = new();
    public List<ActionDto> Actions { get; set; } = new();
}

// State data for API requests/responses
public class StateDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

// Action data for API requests/responses
public class ActionDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> FromStates { get; set; } = new();
    public string ToState { get; set; } = string.Empty;
    public string? Description { get; set; }
}

/// <summary>
/// Request to start a new workflow instance
/// </summary>
public class StartWorkflowInstanceRequest
{
    public string DefinitionId { get; set; } = string.Empty;
}

/// <summary>
/// Request to execute an action on a workflow instance
/// </summary>
public class ExecuteActionRequest
{
    public string ActionId { get; set; } = string.Empty;
}

/// <summary>
/// Response with workflow instance details
/// </summary>
public class WorkflowInstanceResponse
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentStateId { get; set; } = string.Empty;
    public string CurrentStateName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<HistoryEntryDto> History { get; set; } = new();
}

/// <summary>
/// History entry for API responses
/// </summary>
public class HistoryEntryDto
{
    public string ActionId { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string FromStateId { get; set; } = string.Empty;
    public string FromStateName { get; set; } = string.Empty;
    public string ToStateId { get; set; } = string.Empty;
    public string ToStateName { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
