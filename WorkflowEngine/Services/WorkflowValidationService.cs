using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

// Service for validating workflow definitions and operations
public class WorkflowValidationService
{
    // Validates a workflow definition before saving
    public ValidationResult ValidateDefinition(WorkflowDefinition definition)
    {
        var errors = new List<string>();

        // Check for duplicate state IDs
        var stateIds = definition.States.Select(s => s.Id).ToList();
        var duplicateStates = stateIds.GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicate in duplicateStates)
        {
            errors.Add($"Duplicate state ID: {duplicate}");
        }

        // Check for duplicate action IDs
        var actionIds = definition.Actions.Select(a => a.Id).ToList();
        var duplicateActions = actionIds.GroupBy(id => id)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        
        foreach (var duplicate in duplicateActions)
        {
            errors.Add($"Duplicate action ID: {duplicate}");
        }

        // Must have exactly one initial state
        var initialStates = definition.States.Where(s => s.IsInitial).ToList();
        if (initialStates.Count == 0)
        {
            errors.Add("No initial state found - one is required");
        }
        else if (initialStates.Count > 1)
        {
            errors.Add($"Too many initial states ({initialStates.Count}) - only one allowed");
        }

        // Validate actions reference valid states
        var validStateIds = new HashSet<string>(stateIds);
        foreach (var action in definition.Actions)
        {
            // Check fromStates exist
            foreach (var fromState in action.FromStates)
            {
                if (!validStateIds.Contains(fromState))
                {
                    errors.Add($"Action '{action.Id}' has invalid from-state: {fromState}");
                }
            }

            // Check toState exists
            if (!validStateIds.Contains(action.ToState))
            {
                errors.Add($"Action '{action.Id}' has invalid to-state: {action.ToState}");
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    // Validates if an action can be executed on an instance
    public ValidationResult ValidateActionExecution(
        WorkflowDefinition definition, 
        WorkflowInstance instance, 
        string actionId)
    {
        var errors = new List<string>();

        // Find the action
        var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action == null)
        {
            errors.Add($"Action '{actionId}' not found");
            return new ValidationResult(false, errors);
        }

        // Check if action is enabled
        if (!action.Enabled)
        {
            errors.Add($"Action '{actionId}' is disabled");
        }

        // Check current state allows this action
        if (!action.FromStates.Contains(instance.CurrentStateId))
        {
            errors.Add($"Can't execute '{actionId}' from state '{instance.CurrentStateId}'");
        }

        // Check if current state is final
        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
        if (currentState?.IsFinal == true)
        {
            errors.Add($"Can't execute actions on final state '{instance.CurrentStateId}'");
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}

public class ValidationResult
{
    public bool IsValid { get; }
    public List<string> Errors { get; }

    public ValidationResult(bool isValid, List<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }
}
