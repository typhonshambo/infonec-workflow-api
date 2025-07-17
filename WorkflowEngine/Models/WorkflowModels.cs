namespace WorkflowEngine.Models;

// Represents a state in a workflow
public class WorkflowState
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsInitial { get; set; }
    public bool IsFinal { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

// Represents an action that transitions between states
public class WorkflowAction
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public List<string> FromStates { get; set; } = new();
    public string ToState { get; set; } = string.Empty;
    public string? Description { get; set; }
}

// A workflow definition is essentially a template
public class WorkflowDefinition
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<WorkflowState> States { get; set; } = new();
    public List<WorkflowAction> Actions { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? Description { get; set; }
}

// A running instance of a workflow
public class WorkflowInstance
{
    public string Id { get; set; } = string.Empty;
    public string DefinitionId { get; set; } = string.Empty;
    public string CurrentStateId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<WorkflowHistoryEntry> History { get; set; } = new();
}

// Tracks what happened in a workflow instance
public class WorkflowHistoryEntry
{
    public string ActionId { get; set; } = string.Empty;
    public string FromStateId { get; set; } = string.Empty;
    public string ToStateId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
