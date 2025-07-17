using System.Collections.Concurrent;
using WorkflowEngine.Models;

namespace WorkflowEngine.Services;

// In-memory storage for workflow definitions and instances
// Using ConcurrentDictionary for thread safety
// TODO: Replace with Entity Framework + SQLite for production persistence, will do if i get shortlisted T_T
public class WorkflowRepository
{
    private readonly ConcurrentDictionary<string, WorkflowDefinition> _definitions = new();
    private readonly ConcurrentDictionary<string, WorkflowInstance> _instances = new();

    // Workflow Definitions
    public Task<WorkflowDefinition?> GetDefinitionAsync(string id)
    {
        _definitions.TryGetValue(id, out var definition);
        return Task.FromResult(definition);
    }

    public Task<List<WorkflowDefinition>> GetAllDefinitionsAsync()
    {
        return Task.FromResult(_definitions.Values.ToList());
    }

    public Task<WorkflowDefinition> SaveDefinitionAsync(WorkflowDefinition definition)
    {
        if (string.IsNullOrEmpty(definition.Id))
        {
            definition.Id = Guid.NewGuid().ToString();
        }
        
        _definitions[definition.Id] = definition;
        return Task.FromResult(definition);
    }

    // Workflow Instances
    public Task<WorkflowInstance?> GetInstanceAsync(string id)
    {
        _instances.TryGetValue(id, out var instance);
        return Task.FromResult(instance);
    }

    public Task<List<WorkflowInstance>> GetAllInstancesAsync()
    {
        return Task.FromResult(_instances.Values.ToList());
    }

    public Task<List<WorkflowInstance>> GetInstancesByDefinitionAsync(string definitionId)
    {
        var instances = _instances.Values
            .Where(i => i.DefinitionId == definitionId)
            .ToList();
        return Task.FromResult(instances);
    }

    public Task<WorkflowInstance> SaveInstanceAsync(WorkflowInstance instance)
    {
        if (string.IsNullOrEmpty(instance.Id))
        {
            instance.Id = Guid.NewGuid().ToString();
        }
        
        _instances[instance.Id] = instance;
        return Task.FromResult(instance);
    }
}
