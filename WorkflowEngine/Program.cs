using WorkflowEngine.Services;
using WorkflowEngine.DTOs;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.OpenApi.Scalar;
using Microsoft.AspNetCore.OpenApi.Scalar.Models;


var builder = WebApplication.CreateBuilder(args);

// Dependency injection
builder.Services.AddSingleton<WorkflowRepository>();
builder.Services.AddSingleton<WorkflowValidationService>();
builder.Services.AddSingleton<WorkflowService>();
builder.Services.AddEndpointsApiExplorer();

// SCALAR CONFIGURATION (active)
builder.Services.AddScalar(options =>
{
    options.Title = "Workflow Engine API";
    options.Version = "v1";
    options.Description = @"
A workflow state machine that actually works (hopefully).

You can:
- Create workflow definitions 
- Start instances from those definitions
- Execute actions to move things around
- See what's happening";
    options.Contact = new()
    {
        Name = "shambo",
        Email = "shamboc04@gmail.com",
        Url = new Uri("https://github.com/Chimankarparag/infonec-workflow-api")
    };
    options.Documentation = new ScalarDocumentationOptions
    {
        DefaultTheme = ScalarTheme.Dark,
        TagGroups = new[]
        {
            new ScalarTagGroup
            {
                Name = "Workflow Management",
                Tags = new[] { "Definitions", "Instances" }
            }
        },
        Examples = true,
        ShowSchemas = true,
        DefaultExpanded = true,
        EnableCodeCopy = true
    };
    options.Servers = new[]
    {
        new ScalarServer 
        { 
            Url = "http://localhost:5000",
            Description = "Development Server"
        }
    };
    options.UI = new ScalarUIOptions
    {
        PageTitle = "Workflow Engine API Documentation",
        Search = new ScalarSearchOptions
        {
            Enabled = true,
            MinLength = 3
        },
        Layout = new ScalarLayoutOptions
        {
            ShowApiTitle = true,
            ShowModels = true
        }
    };
});

/* SWAGGER CONFIGURATION (commented)
// builder.Services.AddSwaggerGen(c =>
// {
//     c.SwaggerDoc("v1", new OpenApiInfo 
//     { 
//         Title = "Workflow Engine API",
//         Version = "v1",
//         Description = @"
// A workflow state machine that actually works (hopefully).
// You can:
// - Create workflow definitions 
// - Start instances from those definitions
// - Execute actions to move things around
// - See what's happening
// ",
//         Contact = new OpenApiContact 
//         { 
//             Name = "shambo",
//             Email = "shamboc04@gmail.com" 
//         }
//     });
// });
*/


var app = builder.Build();

// SCALAR MIDDLEWARE (active)
app.UseScalar(options =>
{
    options.Path = "/api";
    options.UI = new ScalarUIOptions
    {
        PageTitle = "Workflow Engine API Documentation",
        PrimaryColor = "#0066cc",
        Layout = new ScalarLayoutOptions
        {
            ShowApiTitle = true,
            ShowApiVersion = true,
            ShowModels = true,
            ShowExamples = true,
            Navigation = new ScalarNavigationOptions
            {
                Sticky = true,
                DefaultExpanded = true
            }
        },
        Search = new ScalarSearchOptions
        {
            Enabled = true,
            MinLength = 3,
            IncludeDescription = true
        },
        Request = new ScalarRequestOptions
        {
            EnableTrying = true,
            EnableDeepLinking = true,
            ShowCurl = true,
            ShowPowershell = true,
            WithCredentials = true
        },
        Response = new ScalarResponseOptions
        {
            ShowRaw = true,
            ShowHeaders = true,
            PrettyPrint = true
        }
    };
    options.Authorization = new ScalarAuthorizationOptions
    {
        Enabled = true,
        DefaultScheme = "Bearer",
        PersistAuthorization = true
    };
});

// Optional: Security Headers for Scalar
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    await next();
});

/* SWAGGER MIDDLEWARE (commented)
// app.UseSwagger(c => 
// {
//     c.RouteTemplate = "api/{documentName}/openapi.json";
// });
// app.UseSwaggerUI(c =>
// {
//     c.RoutePrefix = "api";
//     c.SwaggerEndpoint("/api/v1/openapi.json", "Workflow Engine API v1");
//     c.DocumentTitle = "Workflow Engine API";
//     c.DefaultModelExpandDepth(3);
//     c.DefaultModelRendering(Swashbuckle.AspNetCore.SwaggerUI.ModelRendering.Model);
//     c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
//     c.EnableDeepLinking();
//     c.DisplayRequestDuration();
// });
*/


// WORKFLOW DEFINITION ENDPOINTS
app.MapGet("/api/workflows/definitions", async (WorkflowService workflowService) =>
{
    var definitions = await workflowService.GetAllDefinitionsAsync();
    return Results.Ok(definitions);
})
.WithTags("Definitions")
.WithSummary("List all workflow definitions");

app.MapGet("/api/workflows/definitions/{id}", async (string id, WorkflowService workflowService) =>
{
    var definition = await workflowService.GetDefinitionAsync(id);
    return definition != null ? Results.Ok(definition) : Results.NotFound();
})
.WithTags("Definitions")
.WithSummary("Get workflow definition by ID");

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

// WORKFLOW INSTANCES
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