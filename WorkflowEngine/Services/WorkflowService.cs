using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WorkflowEngine.Models;
using WorkflowEngine.DTOs;

namespace WorkflowEngine.Services
{
    /// <summary>
    /// Main service for workflow operations. Handles creation, execution and management of workflows.
    /// </summary>
    public class WorkflowService : IDisposable
    {
        private readonly WorkflowRepository _repository;
        private readonly WorkflowValidationService _validationService;
        private readonly SemaphoreSlim _lock;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the WorkflowService
        /// </summary>
        /// <param name="repository">Repository for persistent storage</param>
        /// <param name="validationService">Service for validating workflow operations</param>
        /// <exception cref="ArgumentNullException">Thrown when repository or validationService is null</exception>
        public WorkflowService(WorkflowRepository repository, WorkflowValidationService validationService)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _validationService = validationService ?? throw new ArgumentNullException(nameof(validationService));
            _lock = new SemaphoreSlim(1, 1);
        }

        /// <summary>
        /// Creates a new workflow definition
        /// </summary>
        public async Task<(bool Success, WorkflowDefinition? Definition, List<string> Errors)> 
            CreateDefinitionAsync(CreateWorkflowDefinitionRequest? request)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            try
            {
                if (request == null)
                {
                    return (false, null, new List<string> { "Request cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    return (false, null, new List<string> { "Workflow name is required" });
                }

                if (request.States == null || !request.States.Any())
                {
                    return (false, null, new List<string> { "At least one state is required" });
                }

                if (request.Actions == null || !request.Actions.Any())
                {
                    return (false, null, new List<string> { "At least one action is required" });
                }

                // Check for null or invalid state/action references
                var invalidStates = request.States.Where(s => string.IsNullOrWhiteSpace(s.Id) || string.IsNullOrWhiteSpace(s.Name)).ToList();
                if (invalidStates.Any())
                {
                    return (false, null, new List<string> { "All states must have valid IDs and names" });
                }

                var invalidActions = request.Actions.Where(a => 
                    string.IsNullOrWhiteSpace(a.Id) || 
                    string.IsNullOrWhiteSpace(a.Name) ||
                    string.IsNullOrWhiteSpace(a.ToState) ||
                    a.FromStates == null ||
                    !a.FromStates.Any() ||
                    a.FromStates.Any(string.IsNullOrWhiteSpace)).ToList();
                if (invalidActions.Any())
                {
                    return (false, null, new List<string> { "All actions must have valid IDs, names, and state references" });
                }

                await _lock.WaitAsync();
                try
                {
                    // Check for existing workflow with the same name
                    var existingDefinitions = await _repository.GetAllDefinitionsAsync();
                    if (existingDefinitions.Any(d => d.Name.Equals(request.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
                    {
                        return (false, null, new List<string> { $"A workflow with the name '{request.Name.Trim()}' already exists" });
                    }

                    // Convert DTOs to domain models
                    var definition = new WorkflowDefinition
                    {
                        Id = Guid.NewGuid().ToString(),
                        Name = request.Name.Trim(),
                        Description = request.Description?.Trim(),
                        States = request.States.Select(s => new WorkflowState
                        {
                            Id = s.Id.Trim(),
                            Name = s.Name.Trim(),
                            IsInitial = s.IsInitial,
                            IsFinal = s.IsFinal,
                            Enabled = s.Enabled,
                            Description = s.Description?.Trim()
                        }).ToList(),
                        Actions = request.Actions.Select(a => new WorkflowAction
                        {
                            Id = a.Id.Trim(),
                            Name = a.Name.Trim(),
                            Enabled = a.Enabled,
                            FromStates = a.FromStates.Select(fs => fs.Trim()).ToList(),
                            ToState = a.ToState.Trim(),
                            Description = a.Description?.Trim()
                        }).ToList()
                    };

                    // Validate business rules
                    var validation = _validationService.ValidateDefinition(definition);
                    if (!validation.IsValid)
                    {
                        return (false, null, validation.Errors.Select(e => e.Message).ToList());
                    }

                    // Save the definition
                    var savedDefinition = await _repository.SaveDefinitionAsync(definition);
                    return (true, savedDefinition, new List<string>());
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception)
            {
                // Log the exception here
                return (false, null, new List<string> { "An unexpected error occurred while creating the workflow definition" });
            }
        }

        /// <summary>
        /// Gets a workflow definition by ID
        /// </summary>
        public async Task<WorkflowDefinition?> GetDefinitionAsync(string id)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));
            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _repository.GetDefinitionAsync(id);
        }

        /// <summary>
        /// Gets all workflow definitions
        /// </summary>
        public async Task<List<WorkflowDefinition>> GetAllDefinitionsAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            return await _repository.GetAllDefinitionsAsync();
        }

        /// <summary>
        /// Starts a new workflow instance
        /// </summary>
        public async Task<(bool Success, WorkflowInstance? Instance, List<string> Errors)> 
            StartInstanceAsync(StartWorkflowInstanceRequest? request)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            try
            {
                if (request == null)
                {
                    return (false, null, new List<string> { "Request cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.DefinitionId))
                {
                    return (false, null, new List<string> { "Workflow definition ID cannot be empty" });
                }

                await _lock.WaitAsync();
                try
                {
                    // Get the definition
                    var definition = await _repository.GetDefinitionAsync(request.DefinitionId);
                    if (definition == null)
                    {
                        return (false, null, new List<string> { $"Workflow definition '{request.DefinitionId}' not found" });
                    }

                    // Validate the definition
                    var validation = _validationService.ValidateDefinition(definition);
                    if (!validation.IsValid)
                    {
                        return (false, null, validation.Errors.Select(e => e.Message).ToList());
                    }

                    // Find the initial state
                    var initialState = definition.States.FirstOrDefault(s => s.IsInitial);
                    if (initialState == null)
                    {
                        return (false, null, new List<string> { "Workflow definition has no initial state" });
                    }

                    if (!initialState.Enabled)
                    {
                        return (false, null, new List<string> { $"Initial state '{initialState.Id}' is disabled" });
                    }

                    // Create the instance
                    var instance = new WorkflowInstance
                    {
                        Id = Guid.NewGuid().ToString(),
                        DefinitionId = request.DefinitionId,
                        CurrentStateId = initialState.Id,
                        CreatedAt = DateTime.UtcNow,
                        History = new List<WorkflowHistoryEntry>()
                    };

                    var savedInstance = await _repository.SaveInstanceAsync(instance);
                    return (true, savedInstance, new List<string>());
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception)
            {
                // Log the exception here
                return (false, null, new List<string> { "An unexpected error occurred while starting the workflow instance" });
            }
        }

        /// <summary>
        /// Executes an action on a workflow instance
        /// </summary>
        public async Task<(bool Success, WorkflowInstance? Instance, List<string> Errors)> 
            ExecuteActionAsync(string instanceId, ExecuteActionRequest? request)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            try
            {
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    return (false, null, new List<string> { "Instance ID cannot be empty" });
                }

                if (request == null)
                {
                    return (false, null, new List<string> { "Request cannot be null" });
                }

                if (string.IsNullOrWhiteSpace(request.ActionId))
                {
                    return (false, null, new List<string> { "Action ID cannot be empty" });
                }

                await _lock.WaitAsync();
                try
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
                        return (false, null, validation.Errors.Select(e => e.Message).ToList());
                    }

                    // Find the action
                    var action = definition.Actions.FirstOrDefault(a => a.Id == request.ActionId);
                    if (action == null)
                    {
                        return (false, null, new List<string> { $"Action '{request.ActionId}' not found" });
                    }

                    // Verify states exist and are enabled
                    var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
                    if (currentState == null)
                    {
                        return (false, null, new List<string> { $"Current state '{instance.CurrentStateId}' not found" });
                    }

                    if (!currentState.Enabled)
                    {
                        return (false, null, new List<string> { $"Current state '{currentState.Id}' is disabled" });
                    }

                    var targetState = definition.States.FirstOrDefault(s => s.Id == action.ToState);
                    if (targetState == null)
                    {
                        return (false, null, new List<string> { $"Target state '{action.ToState}' not found" });
                    }

                    if (!targetState.Enabled)
                    {
                        return (false, null, new List<string> { $"Target state '{targetState.Id}' is disabled" });
                    }

                    // Execute the action
                    var historyEntry = new WorkflowHistoryEntry
                    {
                        ActionId = request.ActionId,
                        FromStateId = instance.CurrentStateId,
                        ToStateId = action.ToState,
                        Timestamp = DateTime.UtcNow
                    };

                    instance.CurrentStateId = action.ToState;
                    instance.History.Add(historyEntry);

                    var savedInstance = await _repository.SaveInstanceAsync(instance);
                    return (true, savedInstance, new List<string>());
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception)
            {
                return (false, null, new List<string> { "An unexpected error occurred while executing the action" });
            }
        }

        /// <summary>
        /// Gets a workflow instance by ID
        /// </summary>
        public async Task<WorkflowInstance?> GetInstanceAsync(string id)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));
            if (string.IsNullOrWhiteSpace(id)) return null;

            return await _repository.GetInstanceAsync(id);
        }

        /// <summary>
        /// Gets all workflow instances
        /// </summary>
        public async Task<List<WorkflowInstance>> GetAllInstancesAsync()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            return await _repository.GetAllInstancesAsync();
        }

        /// <summary>
        /// Gets detailed information about a workflow instance
        /// </summary>
        public async Task<(bool Success, WorkflowInstanceResponse? Response, List<string> Errors)> 
            GetInstanceDetailsAsync(string instanceId)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));

            try
            {
                if (string.IsNullOrWhiteSpace(instanceId))
                {
                    return (false, null, new List<string> { "Instance ID cannot be empty" });
                }

                var instance = await GetInstanceAsync(instanceId);
                if (instance == null)
                {
                    return (false, null, new List<string> { $"Workflow instance '{instanceId}' not found" });
                }

                var definition = await GetDefinitionAsync(instance.DefinitionId);
                if (definition == null)
                {
                    return (false, null, new List<string> { $"Workflow definition '{instance.DefinitionId}' not found" });
                }

                var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
                if (currentState == null)
                {
                    return (false, null, new List<string> { $"Current state '{instance.CurrentStateId}' not found" });
                }

                var response = new WorkflowInstanceResponse
                {
                    Id = instance.Id,
                    DefinitionId = instance.DefinitionId,
                    CurrentStateId = instance.CurrentStateId,
                    CurrentStateName = currentState.Name,
                    CreatedAt = instance.CreatedAt,
                    History = new List<HistoryEntryDto>()
                };

                foreach (var h in instance.History)
                {
                    var action = definition.Actions.FirstOrDefault(a => a.Id == h.ActionId);
                    if (action == null) continue;

                    var fromState = definition.States.FirstOrDefault(s => s.Id == h.FromStateId);
                    if (fromState == null) continue;

                    var toState = definition.States.FirstOrDefault(s => s.Id == h.ToStateId);
                    if (toState == null) continue;

                    response.History.Add(new HistoryEntryDto
                    {
                        ActionId = h.ActionId,
                        ActionName = action.Name,
                        FromStateId = h.FromStateId,
                        FromStateName = fromState.Name,
                        ToStateId = h.ToStateId,
                        ToStateName = toState.Name,
                        Timestamp = h.Timestamp
                    });
                }

                return (true, response, new List<string>());
            }
            catch (Exception)
            {
                return (false, null, new List<string> { "An unexpected error occurred while retrieving instance details" });
            }
        }

        /// <summary>
        /// Checks if a workflow instance exists
        /// </summary>
        public async Task<bool> InstanceExistsAsync(string instanceId)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));
            if (string.IsNullOrWhiteSpace(instanceId)) return false;

            var instance = await GetInstanceAsync(instanceId);
            return instance != null;
        }

        /// <summary>
        /// Validates instance exists and is in a valid state
        /// </summary>
        public async Task<(bool Valid, List<string> Errors)> ValidateInstanceAsync(string instanceId)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(WorkflowService));
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(instanceId))
            {
                errors.Add("Instance ID cannot be empty");
                return (false, errors);
            }

            var instance = await GetInstanceAsync(instanceId);
            if (instance == null)
            {
                errors.Add($"Workflow instance '{instanceId}' not found");
                return (false, errors);
            }

            var definition = await GetDefinitionAsync(instance.DefinitionId);
            if (definition == null)
            {
                errors.Add($"Workflow definition '{instance.DefinitionId}' not found");
                return (false, errors);
            }

            var currentState = definition.States.FirstOrDefault(s => s.Id == instance.CurrentStateId);
            if (currentState == null)
            {
                errors.Add($"Current state '{instance.CurrentStateId}' not found in workflow definition");
                return (false, errors);
            }

            if (!currentState.Enabled)
            {
                errors.Add($"Current state '{currentState.Id}' is disabled");
                return (false, errors);
            }

            return (true, errors);
        }

        /// <summary>
        /// Releases resources used by the service
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Protected implementation of Dispose pattern
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _lock.Dispose();
            }

            _disposed = true;
        }
    }
}