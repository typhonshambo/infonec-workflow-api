using WorkflowEngine.Services;
using WorkflowEngine.DTOs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Configure dependency injection
builder.Services.AddSingleton<WorkflowRepository>();
builder.Services.AddSingleton<WorkflowValidationService>();
builder.Services.AddSingleton<WorkflowService>();

// Configure OpenAPI with custom UI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Workflow Engine API",
        Version = "v1",
        Description = @"
A workflow state machine that actually works (hopefully).

You can:
- Create workflow definitions 
- Start instances from those definitions
- Execute actions to move things around
- See what's happening
",
        Contact = new OpenApiContact 
        { 
            Name = "shambo",
            Email = "shamboc04@gmail.com" 
        }
    });
});

var app = builder.Build();

// Configure middleware with custom path and UI options
app.UseSwagger(c => 
{
    c.RouteTemplate = "api/{documentName}/openapi.json";
});

app.UseSwaggerUI(c =>
{
    c.RoutePrefix = "api";
    c.SwaggerEndpoint("/api/v1/openapi.json", "Workflow Engine API v1");
    c.DocumentTitle = "Workflow Engine API";
    c.DefaultModelExpandDepth(3);
    c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
    c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    c.EnableDeepLinking();
    c.DisplayRequestDuration();
});

// WORKFLOW DEFINITION ENDPOINTS

// Get all definitions
app.MapGet("/api/workflows/definitions", async (WorkflowService workflowService) =>
{
    var definitions = await workflowService.GetAllDefinitionsAsync();
    return Results.Ok(definitions);
})
.WithTags("Definitions")
.WithSummary("List all workflow definitions");

// Get one definition
app.MapGet("/api/workflows/definitions/{id}", async (string id, WorkflowService workflowService) =>
{
    var definition = await workflowService.GetDefinitionAsync(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
})
.WithTags("Definitions")
.WithSummary("Get workflow definition by ID");

// Create new definition
app.MapPost("/api/workflows/definitions", async (CreateWorkflowDefinitionRequest request, WorkflowService workflowService) =>
{
    var (success, definition, errors) = await workflowService.CreateDefinitionAsync(request);
    
    if (success)
    {
        return Results.Created($"/api/workflows/definitions/{definition!.Id}", definition);
    }
    
    return Results.BadRequest(new { errors });
})
.WithTags("Definitions")
.WithSummary("Create workflow definition")
.WithDescription("Remember: exactly one initial state required!");

//WORKFLOW INSTANCES

// Start new instance
app.MapPost("/api/workflows/instances", async (StartWorkflowInstanceRequest request, WorkflowService workflowService) =>
{
    var (success, instance, errors) = await workflowService.StartInstanceAsync(request);
    
    if (success)
    {
        var (detailsSuccess, response, detailsErrors) = await workflowService.GetInstanceDetailsAsync(instance!.Id);
        if (detailsSuccess)
        {
            return Results.Created($"/api/workflows/instances/{instance.Id}", response);
        }
        return Results.BadRequest(new { errors = detailsErrors });
    }
    
    return Results.BadRequest(new { errors });
})
.WithTags("Instances")
.WithSummary("Start workflow instance");

// Get all instances
app.MapGet("/api/workflows/instances", async (WorkflowService workflowService) =>
{
    var instances = await workflowService.GetAllInstancesAsync();
    var responses = new List<object>();
    
    foreach (var instance in instances)
    {
        var (success, response, errors) = await workflowService.GetInstanceDetailsAsync(instance.Id);
        if (success)
        {
            responses.Add(response!);
        }
    }
    
    return Results.Ok(responses);
})
.WithTags("Instances")
.WithSummary("List all instances");

// Get one instance
app.MapGet("/api/workflows/instances/{id}", async (string id, WorkflowService workflowService) =>
{
    var (success, response, errors) = await workflowService.GetInstanceDetailsAsync(id);
    if (success)
    {
        return Results.Ok(response);
    }
    return Results.NotFound(new { errors });
})
.WithTags("Instances")
.WithSummary("Get instance by ID");

// Execute action - the fun part
app.MapPost("/api/workflows/instances/{id}/actions", async (string id, ExecuteActionRequest request, WorkflowService workflowService) =>
{
    var (success, instance, errors) = await workflowService.ExecuteActionAsync(id, request);
    
    if (success)
    {
        var (detailsSuccess, response, detailsErrors) = await workflowService.GetInstanceDetailsAsync(instance!.Id);
        if (detailsSuccess)
        {
            return Results.Ok(response);
        }
        return Results.BadRequest(new { errors = detailsErrors });
    }
    
    return Results.BadRequest(new { errors });
})
.WithTags("Instances")
.WithSummary("Execute action on instance")
.WithDescription("Move your instance to next state - validation applies!");

app.Run();