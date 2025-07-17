# Swagger UI Testing Guide

## Quick Start

1. **Start the application:**
   ```bash
   cd WorkflowEngine
   dotnet run
   ```

2. **Open Swagger UI:** `http://localhost:5000/swagger`

## Step-by-Step Testing

### 1. Create a Workflow Definition

**Endpoint:** `POST /api/workflows/definitions`

**JSON Payload:**
```json
{
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
    }
  ]
}
```

**Expected Result:** Status 201, returns definition with generated ID

---

### 2. Start a Workflow Instance

**Endpoint:** `POST /api/workflows/instances`

**JSON Payload:**
```json
{
  "definitionId": "0c17dd9e-8960-467d-b1de-325132caae76"
}
```

*Note: Replace with your actual definition ID from step 1*

**Expected Result:** Status 201, instance starts in "draft" state

---

### 3. Execute Actions

**Endpoint:** `POST /api/workflows/instances/{instanceId}/actions`

**Submit for Review:**
```json
{
  "actionId": "submit"
}
```

**Approve Document:**
```json
{
  "actionId": "approve"
}
```

**Expected Results:** 
- First action moves to "review" state
- Second action moves to "approved" state (final)

---

### 4. Test Validation Errors

**Try Invalid Action on Final State:**
```json
{
  "actionId": "submit"
}
```

**Expected Result:** Status 400 with error message

---

## Common Test Scenarios

### Valid Operations
- Create workflow with proper states/actions
- Start instance from valid definition
- Execute actions following valid transitions
- View instance history

### Invalid Operations (may Fail)
- Duplicate state IDs in definition
- No initial state in definition
- Execute disabled actions
- Execute actions from wrong states
- Execute actions on final states

## Common Issues (i faced, i were dumb)

 - **404 on Swagger:** Make sure application is running and Swagger is enabled
 - **400 Bad Request:** Check JSON syntax and required fields
 - **Instance Not Found:** Verify you're using the correct instance ID from the creation response
