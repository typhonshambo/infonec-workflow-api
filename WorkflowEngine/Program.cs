using WorkflowEngine.Services;
using WorkflowEngine.DTOs;

var builder = WebApplication.CreateBuilder(args);

// Configure dependency injection
builder.Services.AddSingleton<WorkflowRepository>();
builder.Services.AddSingleton<WorkflowValidationService>();
builder.Services.AddSingleton<WorkflowService>();

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "Workflow Engine API", 
        Version = "v1",
        Description = "A configurable workflow state machine API"
    });
});

var app = builder.Build();

// Configure middleware
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Workflow Engine API v1");
    c.RoutePrefix = "swagger";
});

// WORKFLOW DEFINITION ENDPOINTS

// List all workflow definitions
app.MapGet("/api/workflows/definitions", async (WorkflowService workflowService) =>
{
    var definitions = await workflowService.GetAllDefinitionsAsync();
    return Results.Ok(definitions);
})
.WithName("GetAllWorkflowDefinitions")
.WithSummary("Get all workflow definitions");

// Get specific workflow definition
app.MapGet("/api/workflows/definitions/{id}", async (string id, WorkflowService workflowService) =>
{
    var definition = await workflowService.GetDefinitionAsync(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
})
.WithName("GetWorkflowDefinition")
.WithSummary("Get a specific workflow definition by ID");

// Create new workflow definition
app.MapPost("/api/workflows/definitions", async (CreateWorkflowDefinitionRequest request, WorkflowService workflowService) =>
{
    var (success, definition, errors) = await workflowService.CreateDefinitionAsync(request);
    
    if (success)
    {
        return Results.Created($"/api/workflows/definitions/{definition!.Id}", definition);
    }
    
    return Results.BadRequest(new { errors });
})
.WithName("CreateWorkflowDefinition")
.WithSummary("Create a new workflow definition");

//WORKFLOW INSTANCE ENDPOINTS

// WORKFLOW INSTANCE ENDPOINTS

// POST /api/workflows/instances - Start new workflow instance
app.MapPost("/api/workflows/instances", async (StartWorkflowInstanceRequest request, WorkflowService workflowService) =>
{
    var (success, instance, errors) = await workflowService.StartInstanceAsync(request);
    
    if (success)
    {
        var response = await workflowService.GetInstanceResponseAsync(instance!.Id);
        return Results.Created($"/api/workflows/instances/{instance.Id}", response);
    }
    
    return Results.BadRequest(new { errors });
})
.WithName("StartWorkflowInstance")
.WithSummary("Start a new workflow instance");

// GET /api/workflows/instances - List all workflow instances
app.MapGet("/api/workflows/instances", async (WorkflowService workflowService) =>
{
    var instances = await workflowService.GetAllInstancesAsync();
    var responses = new List<object>();
    
    foreach (var instance in instances)
    {
        var response = await workflowService.GetInstanceResponseAsync(instance.Id);
        if (response != null)
        {
            responses.Add(response);
        }
    }
    
    return Results.Ok(responses);
})
.WithName("GetAllWorkflowInstances")
.WithSummary("Get all workflow instances");

// GET /api/workflows/instances/{id} - Get specific workflow instance  
app.MapGet("/api/workflows/instances/{id}", async (string id, WorkflowService workflowService) =>
{
    var response = await workflowService.GetInstanceResponseAsync(id);
    return response != null ? Results.Ok(response) : Results.NotFound();
})
.WithName("GetWorkflowInstance")
.WithSummary("Get a specific workflow instance by ID");

// POST /api/workflows/instances/{id}/actions - Execute action on instance
app.MapPost("/api/workflows/instances/{id}/actions", async (string id, ExecuteActionRequest request, WorkflowService workflowService) =>
{
    var (success, instance, errors) = await workflowService.ExecuteActionAsync(id, request);
    
    if (success)
    {
        var response = await workflowService.GetInstanceResponseAsync(instance!.Id);
        return Results.Ok(response);
    }
    
    return Results.BadRequest(new { errors });
})
.WithName("ExecuteWorkflowAction")
.WithSummary("Execute an action on a workflow instance");

app.Run();
