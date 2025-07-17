using WorkflowEngine.Models;
using WorkflowEngine.DTOs;

namespace WorkflowEngine.Services;

// Main service for workflow operations
public class WorkflowService
{
    private readonly WorkflowRepository _repository;
    private readonly WorkflowValidationService _validationService;

    public WorkflowService(WorkflowRepository repository, WorkflowValidationService validationService)
    {
        _repository = repository;
        _validationService = validationService;
    }

    // Creates a new workflow definition
    public async Task<(bool Success, WorkflowDefinition? Definition, List<string> Errors)> 
        CreateDefinitionAsync(CreateWorkflowDefinitionRequest request)
    {
        // Convert DTOs to domain models
        var definition = new WorkflowDefinition
        {
            Name = request.Name,
            Description = request.Description,
            States = request.States.Select(s => new WorkflowState
            {
                Id = s.Id,
                Name = s.Name,
                IsInitial = s.IsInitial,
                IsFinal = s.IsFinal,
                Enabled = s.Enabled,
                Description = s.Description
            }).ToList(),
            Actions = request.Actions.Select(a => new WorkflowAction
            {
                Id = a.Id,
                Name = a.Name,
                Enabled = a.Enabled,
                FromStates = a.FromStates,
                ToState = a.ToState,
                Description = a.Description
            }).ToList()
        };

        // Validate business rules
        var validation = _validationService.ValidateDefinition(definition);
        if (!validation.IsValid)
        {
            return (false, null, validation.Errors);
        }

        // Save it
        var savedDefinition = await _repository.SaveDefinitionAsync(definition);
        return (true, savedDefinition, new List<string>());
    }

    // Gets a workflow definition by ID
    public async Task<WorkflowDefinition?> GetDefinitionAsync(string id)
    {
        return await _repository.GetDefinitionAsync(id);
    }

    // Gets all workflow definitions
    public async Task<List<WorkflowDefinition>> GetAllDefinitionsAsync()
    {
        return await _repository.GetAllDefinitionsAsync();
    }

    // Starts a new workflow instance
    public async Task<(bool Success, WorkflowInstance? Instance, List<string> Errors)> 
        StartInstanceAsync(StartWorkflowInstanceRequest request)
    {
        // Get the definition
        var definition = await _repository.GetDefinitionAsync(request.DefinitionId);
        if (definition == null)
        {
            return (false, null, new List<string> { $"Workflow definition '{request.DefinitionId}' not found" });
        }

        // Find the initial state
        var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
        if (initialState == null)
        {
            return (false, null, new List<string> { "Workflow definition has no initial state" });
        }

        // Create the instance
        var instance = new WorkflowInstance
        {
            DefinitionId = request.DefinitionId,
            CurrentStateId = initialState.Id
        };

        var savedInstance = await _repository.SaveInstanceAsync(instance);
        return (true, savedInstance, new List<string>());
    }

    // Executes an action on a workflow instance
    public async Task<(bool Success, WorkflowInstance? Instance, List<string> Errors)> 
        ExecuteActionAsync(string instanceId, ExecuteActionRequest request)
    {
        // Get the instance
        var instance = await _repository.GetInstanceAsync(instanceId);
        if (instance == null)
        {
            return (false, null, new List<string> { $"Workflow instance '{instanceId}' not found" });
        }

        // Get the definition
        var definition = await _repository.GetDefinitionAsync(instance.DefinitionId);
        if (definition == null)
        {
            return (false, null, new List<string> { $"Workflow definition '{instance.DefinitionId}' not found" });
        }

        // Validate the action execution
        var validation = _validationService.ValidateActionExecution(definition, instance, request.ActionId);
        if (!validation.IsValid)
        {
            return (false, null, validation.Errors);
        }

        // Find the action
        var action = definition.Actions.First(a => a.Id == request.ActionId);

        // Execute the action and record it
        var historyEntry = new WorkflowHistoryEntry
        {
            ActionId = request.ActionId,
            FromStateId = instance.CurrentStateId,
            ToStateId = action.ToState,
            Timestamp = DateTime.UtcNow
        };

        // Update state
        instance.CurrentStateId = action.ToState;
        instance.History.Add(historyEntry);

        // TODO: Add workflow event notifications for external systems, will do if i get shortlisted T_T
        // TODO: Add action execution middleware for logging/auditing, will do if i get shortlisted T_T

        var savedInstance = await _repository.SaveInstanceAsync(instance);
        return (true, savedInstance, new List<string>());
    }

    // Gets a workflow instance by ID
    public async Task<WorkflowInstance?> GetInstanceAsync(string id)
    {
        return await _repository.GetInstanceAsync(id);
    }

    // Gets all workflow instances
    public async Task<List<WorkflowInstance>> GetAllInstancesAsync()
    {
        return await _repository.GetAllInstancesAsync();
    }

    // Converts a workflow instance to a response DTO
    public async Task<WorkflowInstanceResponse?> GetInstanceResponseAsync(string instanceId)
    {
        var instance = await GetInstanceAsync(instanceId);
        if (instance == null) return null;

        var definition = await GetDefinitionAsync(instance.DefinitionId);
        if (definition == null) return null;

        var currentState = definition.States.First(s => s.Id == instance.CurrentStateId);

        return new WorkflowInstanceResponse
        {
            Id = instance.Id,
            DefinitionId = instance.DefinitionId,
            CurrentStateId = instance.CurrentStateId,
            CurrentStateName = currentState.Name,
            CreatedAt = instance.CreatedAt,
            History = instance.History.Select(h =>
            {
                var action = definition.Actions.First(a => a.Id == h.ActionId);
                var fromState = definition.States.First(s => s.Id == h.FromStateId);
                var toState = definition.States.First(s => s.Id == h.ToStateId);

                return new HistoryEntryDto
                {
                    ActionId = h.ActionId,
                    ActionName = action.Name,
                    FromStateId = h.FromStateId,
                    FromStateName = fromState.Name,
                    ToStateId = h.ToStateId,
                    ToStateName = toState.Name,
                    Timestamp = h.Timestamp
                };
            }).ToList()
        };
    }
}
