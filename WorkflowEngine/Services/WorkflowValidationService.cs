using WorkflowEngine.Models;
using System.Collections.Generic;
using System.Linq;

namespace WorkflowEngine.Services;

/// <summary>
/// Service for validating workflow definitions and operations
/// </summary>
public class WorkflowValidationService
{
    /// <summary>
    /// Validates a workflow definition before saving
    /// </summary>
    public ValidationResult ValidateDefinition(WorkflowDefinition? definition)
    {
        var errors = new List<ValidationError>();

        if (definition == null)
        {
            return new ValidationResult(false, new[] { new ValidationError("Workflow definition cannot be null") });
        }

        ValidateBasicProperties(definition, errors);
        ValidateStates(definition, errors);
        ValidateActions(definition, errors);
        ValidateStateTransitions(definition, errors);

        return new ValidationResult(
            errors.All(e => e.Severity != ValidationSeverity.Error), 
            errors);
    }

    private void ValidateBasicProperties(WorkflowDefinition definition, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(definition.Id))
        {
            errors.Add(new ValidationError("Workflow ID cannot be empty"));
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            errors.Add(new ValidationError("Workflow name cannot be empty"));
        }
    }

    private void ValidateStates(WorkflowDefinition definition, List<ValidationError> errors)
    {
        var stateIds = new HashSet<string>();
        
        foreach (var state in definition.States)
        {
            if (string.IsNullOrWhiteSpace(state.Id))
            {
                errors.Add(new ValidationError("State ID cannot be empty"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(state.Name))
            {
                errors.Add(new ValidationError($"State name cannot be empty for state {state.Id}"));
            }

            if (!stateIds.Add(state.Id))
            {
                errors.Add(new ValidationError($"Duplicate state ID: {state.Id}"));
            }
        }

        // Must have exactly one initial state
        var initialStateCount = definition.States.Count(s => s.IsInitial);
        if (initialStateCount == 0)
        {
            errors.Add(new ValidationError("No initial state found - one is required"));
        }
        else if (initialStateCount > 1)
        {
            errors.Add(new ValidationError($"Too many initial states ({initialStateCount}) - only one allowed"));
        }

        // Check for unreachable states
        var reachableStates = new HashSet<string>();
        var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
        if (initialState != null)
        {
            FindReachableStates(definition, initialState.Id, reachableStates);
        }

        foreach (var state in definition.States.Where(s => !s.IsInitial && !s.IsFinal))
        {
            if (!reachableStates.Contains(state.Id))
            {
                errors.Add(new ValidationError(
                    $"State '{state.Id}' is unreachable from the initial state", 
                    ValidationSeverity.Warning));
            }
        }
    }

    private void FindReachableStates(WorkflowDefinition definition, string startStateId, HashSet<string> reachableStates)
    {
        if (!reachableStates.Add(startStateId))
        {
            return;
        }

        var outgoingActions = definition.Actions
            .Where(a => a.FromStates.Contains(startStateId) && a.Enabled);

        foreach (var action in outgoingActions)
        {
            FindReachableStates(definition, action.ToState, reachableStates);
        }
    }

    private void ValidateActions(WorkflowDefinition definition, List<ValidationError> errors)
    {
        var actionIds = new HashSet<string>();
        var stateIds = new HashSet<string>(definition.States.Select(s => s.Id));

        foreach (var action in definition.Actions)
        {
            if (string.IsNullOrWhiteSpace(action.Id))
            {
                errors.Add(new ValidationError("Action ID cannot be empty"));
                continue;
            }

            if (string.IsNullOrWhiteSpace(action.Name))
            {
                errors.Add(new ValidationError($"Action name cannot be empty for action {action.Id}"));
            }

            if (!actionIds.Add(action.Id))
            {
                errors.Add(new ValidationError($"Duplicate action ID: {action.Id}"));
            }

            if (action.FromStates.Count == 0)
            {
                errors.Add(new ValidationError($"Action '{action.Id}' must have at least one from-state"));
            }

            foreach (var fromState in action.FromStates)
            {
                if (!stateIds.Contains(fromState))
                {
                    errors.Add(new ValidationError($"Action '{action.Id}' has invalid from-state: {fromState}"));
                }
            }

            if (!stateIds.Contains(action.ToState))
            {
                errors.Add(new ValidationError($"Action '{action.Id}' has invalid to-state: {action.ToState}"));
            }

            // Check for self-transitions on final states
            if (action.FromStates.Contains(action.ToState))
            {
                var state = definition.States.FirstOrDefault(s => s.Id == action.ToState);
                if (state?.IsFinal == true)
                {
                    errors.Add(new ValidationError(
                        $"Action '{action.Id}' creates a self-transition on final state '{action.ToState}'",
                        ValidationSeverity.Warning));
                }
            }
        }
    }

    private void ValidateStateTransitions(WorkflowDefinition definition, List<ValidationError> errors)
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();

        void DetectCycle(string stateId)
        {
            visited.Add(stateId);
            recursionStack.Add(stateId);

            var outgoingActions = definition.Actions
                .Where(a => a.FromStates.Contains(stateId));

            foreach (var action in outgoingActions)
            {
                if (!visited.Contains(action.ToState))
                {
                    DetectCycle(action.ToState);
                }
                else if (recursionStack.Contains(action.ToState))
                {
                    errors.Add(new ValidationError(
                        $"Circular reference detected in state transitions through state: {action.ToState}", 
                        ValidationSeverity.Warning));
                }
            }

            recursionStack.Remove(stateId);
        }

        var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
        if (initialState != null)
        {
            DetectCycle(initialState.Id);
        }
    }

    /// <summary>
    /// Validates if an action can be executed on an instance
    /// </summary>
    public ValidationResult ValidateActionExecution(
        WorkflowDefinition? definition,
        WorkflowInstance? instance,
        string actionId)
    {
        var errors = new List<ValidationError>();

        if (definition == null)
        {
            errors.Add(new ValidationError("Workflow definition cannot be null"));
            return new ValidationResult(false, errors);
        }

        if (instance == null)
        {
            errors.Add(new ValidationError("Workflow instance cannot be null"));
            return new ValidationResult(false, errors);
        }

        if (string.IsNullOrWhiteSpace(actionId))
        {
            errors.Add(new ValidationError("Action ID cannot be empty"));
            return new ValidationResult(false, errors);
        }

        // Find the action
        var action = definition.Actions.FirstOrDefault(a => a.Id == actionId);
        if (action == null)
        {
            errors.Add(new ValidationError($"Action '{actionId}' not found"));
            return new ValidationResult(false, errors);
        }

        // Check if action is enabled
        if (!action.Enabled)
        {
            errors.Add(new ValidationError($"Action '{actionId}' is disabled"));
        }

        // Check if current state exists
        var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
        if (currentState == null)
        {
            errors.Add(new ValidationError($"Current state '{instance.CurrentStateId}' not found in workflow definition"));
            return new ValidationResult(false, errors);
        }

        // Check if current state is enabled
        if (!currentState.Enabled)
        {
            errors.Add(new ValidationError($"Current state '{instance.CurrentStateId}' is disabled"));
        }

        // Check current state allows this action
        if (!action.FromStates.Contains(instance.CurrentStateId))
        {
            errors.Add(new ValidationError($"Can't execute '{actionId}' from state '{instance.CurrentStateId}'"));
        }

        // Check if current state is final
        if (currentState.IsFinal)
        {
            errors.Add(new ValidationError($"Can't execute actions on final state '{instance.CurrentStateId}'"));
        }

        return new ValidationResult(errors.Count == 0, errors);
    }
}
