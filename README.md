# Configurable Workflow Engine (State-Machine API)

**Infonetica Software Engineer Intern Take-Home Exercise**

A minimal .NET 8 backend service that manages configurable workflow state machines. Clients can define workflows, start instances, execute actions with validation, and inspect states.



### Prerequisites
- .NET 8 SDK ([download here](https://dotnet.microsoft.com/download/dotnet/8.0))

### Build and Run
```bash
cd WorkflowEngine
dotnet build
dotnet run
```

### Access
- **API Base URL:** `http://localhost:5000`
- **Swagger UI:** `http://localhost:5000/swagger` (interactive API documentation)
- **Sample Testing:** See `WorkflowEngine/SWAGGER_TESTING.md` testing guide with swagger

## Core Concepts 
> you can skip this if you feel bore reading long things :D
### State
- **Id**: Unique identifier
- **Name**: Human-readable name
- **IsInitial**: Exactly one state per workflow must be marked as initial
- **IsFinal**: States marked as final cannot execute further actions
- **Enabled**: Whether the state is active

### Action (Transition)
- **Id**: Unique identifier
- **Name**: Human-readable name
- **FromStates**: List of state IDs from which this action can be executed
- **ToState**: Single target state ID
- **Enabled**: Whether the action can be executed

### Workflow Definition
- Template containing states and actions
- Must have exactly one initial state
- Validates state/action relationships

### Workflow Instance
- Running execution of a workflow definition
- Tracks current state and execution history
- Starts at the definition's initial state

## API Endpoints

### Workflow Definitions

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/workflows/definitions` | List all workflow definitions |
| GET | `/api/workflows/definitions/{id}` | Get specific workflow definition |
| POST | `/api/workflows/definitions` | Create new workflow definition |

### Workflow Instances

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/workflows/instances` | Start new workflow instance |
| GET | `/api/workflows/instances` | List all workflow instances |
| GET | `/api/workflows/instances/{id}` | Get specific workflow instance |
| POST | `/api/workflows/instances/{id}/actions` | Execute action on instance |

## Example Usage

### 1. Create a Simple Approval Workflow

```bash
curl -X POST "http://localhost:5000/api/workflows/definitions" \
-H "Content-Type: application/json" \
-d '{
  "name": "Document Approval",
  "description": "Simple document approval process",
  "states": [
    {
      "id": "draft",
      "name": "Draft",
      "isInitial": true,
      "isFinal": false,
      "enabled": true
    },
    {
      "id": "review",
      "name": "Under Review",
      "isInitial": false,
      "isFinal": false,
      "enabled": true
    },
    {
      "id": "approved",
      "name": "Approved",
      "isInitial": false,
      "isFinal": true,
      "enabled": true
    },
    {
      "id": "rejected",
      "name": "Rejected",
      "isInitial": false,
      "isFinal": true,
      "enabled": true
    }
  ],
  "actions": [
    {
      "id": "submit",
      "name": "Submit for Review",
      "enabled": true,
      "fromStates": ["draft"],
      "toState": "review"
    },
    {
      "id": "approve",
      "name": "Approve Document",
      "enabled": true,
      "fromStates": ["review"],
      "toState": "approved"
    },
    {
      "id": "reject",
      "name": "Reject Document",
      "enabled": true,
      "fromStates": ["review"],
      "toState": "rejected"
    }
  ]
}'
```

### 2. Start a Workflow Instance

```bash
curl -X POST "http://localhost:5000/api/workflows/instances" \
-H "Content-Type: application/json" \
-d '{
  "definitionId": "your-definition-id-here"
}'
```

### 3. Execute an Action

```bash
curl -X POST "http://localhost:5000/api/workflows/instances/your-instance-id/actions" \
-H "Content-Type: application/json" \
-d '{
  "actionId": "submit"
}'
```

### OR, IF YOU'RE LAZY LIKE ME

```
WorkflowEngine/test-api.sh
```

## Validation Rules

The API enforces these validation rules:

### Workflow Definition Validation
- [x] No duplicate state IDs
- [x] No duplicate action IDs  
- [x] Exactly one initial state required
- [x] All action fromStates must reference valid states
- [x] All action toStates must reference valid states

### Action Execution Validation
- [x] Action must exist in workflow definition
- [x] Action must be enabled
- [x] Current state must be in action's `fromStates
- [x] Cannot execute actions on final states

## Architecture & Design Decisions

### Project Structure
```
WorkflowEngine/
├── Models/           # Domain models (State, Action, Definition, Instance)
├── DTOs/            # Data Transfer Objects for API
├── Services/        # Business logic layer
│   ├── WorkflowService.cs          # Main workflow operations
│   ├── WorkflowValidationService.cs # Validation logic
│   └── WorkflowRepository.cs       # Data storage
└── Program.cs       # API endpoints and startup
```

### Key Design Choices

1. **Minimal API Architecture**: Using .NET 8's minimal API pattern for clean, focused endpoints without controller overhead
2. **Thread-Safe Storage**: `ConcurrentDictionary` provides thread-safe in-memory storage suitable for concurrent requests  
3. **Layered Architecture**: Clear separation between DTOs (API), Models (domain), and Services (business logic)
4. **Centralized Validation**: Dedicated `WorkflowValidationService` ensures consistent rule enforcement
5. **Dependency Injection**: Built-in .NET DI container manages service lifecycles and dependencies
6. **Immutable Operations**: State transitions create new history entries without modifying existing data

### Trade-offs & Assumptions

**Assumptions:**
- Workflow instances run single-threaded (no concurrent action execution)
- In-memory storage is sufficient for demo purposes
- No authentication needed for this exercise
- Simple state transitions (no complex business rules)

**Shortcuts taken for time constraints:**
- No database persistence (would use Entity Framework + SQLite in production)
- Basic error handling (would add global exception middleware)  
- No unit tests (would add xUnit tests for validation logic)
- No logging (would integrate Serilog)
- Swagger enabled in all environments (production would be more restrictive)

## Known Limitations

- **Data persistence**: In-memory storage only - data is lost on application restart
- **Concurrency**: No locking mechanism for concurrent instance modifications
- **Scale**: Not optimized for high-throughput scenarios
- **Authentication**: No security layer implemented
- **Bulk operations**: API doesn't support batch operations

## Assumptions Made

1. **Single-threaded execution**: Workflow instances execute actions sequentially
2. **Simple state model**: States are discrete with no sub-states or parallel execution
3. **Immediate transitions**: Actions execute synchronously without delays
4. **Memory constraints**: Acceptable to store all data in memory for demo purposes
5. **API-first design**: No UI needed, Swagger provides adequate interaction

## Environment Notes

- Developed and tested on .NET 8.0
- Compatible with Windows, macOS, and Linux
- Requires minimal dependencies (only Swashbuckle for Swagger)
- Runs on default ASP.NET Core Kestrel server
