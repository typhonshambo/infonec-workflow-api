using WorkflowEngine.Services;
using WorkflowEngine.DTOs;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Configure dependency injection
builder.Services.AddSingleton<WorkflowRepository>();
builder.Services.AddSingleton<WorkflowValidationService>();
builder.Services.AddSingleton<WorkflowService>();

// Add EndpointsApiExplorer for both Swagger and Scalar
builder.Services.AddEndpointsApiExplorer();

/* SWAGGER CONFIGURATION - Currently Active */
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

/* SCALAR CONFIGURATION - Future Use
// First add package when available: Microsoft.AspNetCore.OpenApi.Scalar
// Then add using statements: 
//   using Microsoft.AspNetCore.OpenApi.Scalar;
//   using Microsoft.AspNetCore.OpenApi.Scalar.Models;

builder.Services.AddScalar(options =>
{
    // Basic API Information
    options.Title = "Workflow Engine API";
    options.Version = "v1";
    options.Description = @"
A workflow state machine that actually works (hopefully).

You can:
- Create workflow definitions 
- Start instances from those definitions
- Execute actions to move things around
- See what's happening";
    
    // Contact Information
    options.Contact = new()
    {
        Name = "shambo",
        Email = "shamboc04@gmail.com",
        Url = new Uri("https://github.com/Chimankarparag/infonec-workflow-api")
    };

    // License Information
    options.License = new()
    {
        Name = "MIT",
        Url = new Uri("https://opensource.org/licenses/MIT")
    };

    // Server Configuration
    options.Servers = new[]
    {
        new ScalarServer 
        { 
            Url = "http://localhost:5000",
            Description = "Development Server"
        },
        new ScalarServer 
        { 
            Url = "https://api.yourproduction.com",
            Description = "Production Server"
        }
    };

    // Security Definitions
    options.Security = new ScalarSecurityRequirement[]
    {
        new()
        {
            // Add your security requirements here
            // Example for Bearer token:
            // Name = "Bearer",
            // Scheme = "bearer",
            // BearerFormat = "JWT"
        }
    };

    // Documentation Customization
    options.Documentation = new ScalarDocumentationOptions
    {
        // Dark mode by default
        DefaultTheme = ScalarTheme.Dark,
        
        // Custom navigation grouping
        TagGroups = new[]
        {
            new ScalarTagGroup
            {
                Name = "Workflow Management",
                Tags = new[] { "Definitions", "Instances" }
            }
        },

        // Example request/response pairs
        Examples = true,

        // Show request/response schemas
        ShowSchemas = true,

        // Expand operations by default
        DefaultExpanded = true,

        // Show copy button for code snippets
        EnableCodeCopy = true,

        // Enable try-it-out feature
        TryItOut = new ScalarTryItOutOptions
        {
            Enabled = true,
            // Show request/response headers
            ShowHeaders = true
        }
    };

    // Response Customization
    options.Responses = new ScalarResponseOptions
    {
        // Global response messages
        Global = new Dictionary<string, ScalarResponse>
        {
            ["401"] = new() 
            { 
                Description = "Unauthorized - Authentication required"
            },
            ["403"] = new() 
            { 
                Description = "Forbidden - Insufficient permissions"
            },
            ["500"] = new() 
            { 
                Description = "Internal Server Error - Something went wrong"
            }
        }
    };

    // Request Validation
    options.Validation = new ScalarValidationOptions
    {
        // Enable request validation
        EnableRequestValidation = true,
        
        // Show validation errors in the UI
        ShowValidationErrors = true
    };

    // Performance Options
    options.Performance = new ScalarPerformanceOptions
    {
        // Cache documentation
        EnableCache = true,
        
        // Lazy load schemas
        LazySchemas = true
    };
});
*/

var app = builder.Build();

/* SWAGGER MIDDLEWARE - Currently Active */
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

/* SCALAR MIDDLEWARE - Future Use
app.UseScalar(options =>
{
    // Base path for the Scalar UI
    options.Path = "/api";

    // Route template for OpenAPI JSON
    options.RouteTemplate = "api/{documentName}/openapi.json";

    // Customization Options
    options.UI = new ScalarUIOptions
    {
        // Page title in browser
        PageTitle = "Workflow Engine API Documentation",

        // Favicon URL
        Favicon = "/favicon.ico",

        // Custom CSS URL
        CustomCss = "/css/scalar-custom.css",

        // Custom JavaScript URL
        CustomJs = "/js/scalar-custom.js",

        // Primary color for UI elements
        PrimaryColor = "#0066cc",

        // Custom HTML head content
        HeadContent = @"
            <meta name='description' content='Workflow Engine API Documentation'>
            <meta name='keywords' content='workflow,api,documentation'>
        ",

        // Configure search
        Search = new ScalarSearchOptions
        {
            // Enable search functionality
            Enabled = true,
            // Minimum search term length
            MinLength = 3,
            // Search in descriptions
            IncludeDescription = true
        },

        // Layout options
        Layout = new ScalarLayoutOptions
        {
            // Show/hide elements
            ShowApiTitle = true,
            ShowApiVersion = true,
            ShowModels = true,
            ShowExamples = true,
            
            // Navigation options
            Navigation = new ScalarNavigationOptions
            {
                // Sticky sidebar
                Sticky = true,
                // Expand by default
                DefaultExpanded = true
            }
        },

        // Configure request options
        Request = new ScalarRequestOptions
        {
            // Enable/disable features
            WithCredentials = true,
            EnableTrying = true,
            EnableDeepLinking = true,
            
            // Default timeout
            Timeout = 30000,
            
            // Request samples
            ShowCurl = true,
            ShowPowershell = true
        },

        // Response handling
        Response = new ScalarResponseOptions
        {
            // Show raw response
            ShowRaw = true,
            // Show response headers
            ShowHeaders = true,
            // Pretty print JSON
            PrettyPrint = true
        },

        // Error handling
        Errors = new ScalarErrorOptions
        {
            // Show detailed errors
            ShowDetails = true,
            // Show stack traces in development
            ShowStack = builder.Environment.IsDevelopment()
        }
    });

    // Authorization configuration
    options.Authorization = new ScalarAuthorizationOptions
    {
        // Enable authorization features
        Enabled = true,
        
        // Default auth settings
        DefaultScheme = "Bearer",
        
        // Persist auth tokens
        PersistAuthorization = true,
        
        // Auth UI options
        UI = new ScalarAuthUIOptions
        {
            ShowLogout = true,
            ButtonText = "Authenticate"
        }
    };
});

// Optional: Add security headers for Scalar
app.Use(async (context, next) =>
{
    // Security headers
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    
    await next();
});
*/

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