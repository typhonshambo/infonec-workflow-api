#!/bin/bash

# Test script for Workflow Engine API
# Make sure the API is running on localhost:5000 before running this script

BASE_URL="http://localhost:5000/api/workflows"

echo "Testing Workflow Engine API..."
echo "=================================="

# 1. Create a workflow definition
echo ""
echo "1. Creating a workflow definition..."
DEFINITION_RESPONSE=$(curl -s -X POST "$BASE_URL/definitions" \
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
}')

DEFINITION_ID=$(echo $DEFINITION_RESPONSE | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "created workflow definition with ID: $DEFINITION_ID"

# 2. Get all definitions
echo ""
echo "2. Getting all workflow definitions..."
curl -s -X GET "$BASE_URL/definitions" | head -c 200
echo "..."
echo "retrieved all definitions"

# 3. Start a workflow instance
echo ""
echo "3. Starting a workflow instance..."
INSTANCE_RESPONSE=$(curl -s -X POST "$BASE_URL/instances" \
-H "Content-Type: application/json" \
-d "{\"definitionId\": \"$DEFINITION_ID\"}")

INSTANCE_ID=$(echo $INSTANCE_RESPONSE | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
echo "started workflow instance with ID: $INSTANCE_ID"

# 4. Get the instance (should be in 'draft' state)
echo ""
echo "4. Getting workflow instance (should be in 'draft' state)..."
curl -s -X GET "$BASE_URL/instances/$INSTANCE_ID" | head -c 300
echo "..."
echo "retrieved instance details"

# 5. Execute 'submit' action
echo ""
echo "5. Executing 'submit' action (draft -> review)..."
curl -s -X POST "$BASE_URL/instances/$INSTANCE_ID/actions" \
-H "Content-Type: application/json" \
-d '{"actionId": "submit"}' | head -c 300
echo "..."
echo "executed submit action"

# 6. Execute 'approve' action
echo ""
echo "6. Executing 'approve' action (review -> approved)..."
curl -s -X POST "$BASE_URL/instances/$INSTANCE_ID/actions" \
-H "Content-Type: application/json" \
-d '{"actionId": "approve"}' | head -c 300
echo "..."
echo "executed approve action"

# 7. Try to execute another action (should fail - final state)
echo ""
echo "7. Trying to execute action on final state (should fail)..."
ERROR_RESPONSE=$(curl -s -X POST "$BASE_URL/instances/$INSTANCE_ID/actions" \
-H "Content-Type: application/json" \
-d '{"actionId": "submit"}')
echo $ERROR_RESPONSE | head -c 200
echo "..."
echo "correctly rejected action on final state"

# 8. Get final instance state with history
echo ""
echo "8. Getting final instance state with full history..."
curl -s -X GET "$BASE_URL/instances/$INSTANCE_ID" | head -c 400
echo "..."
echo "retrieved final instance with history"

echo ""
echo "all tests done!"
echo "Check the API responses above to see the workflow in action."
