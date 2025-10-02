#!/bin/bash
# Fetch Render logs via API

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Render Logs Fetcher"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check for API key
if [ -z "$RENDER_API_KEY" ]; then
    echo "⚠️  RENDER_API_KEY not set"
    echo ""
    echo "To get your API key:"
    echo "1. Go to: https://dashboard.render.com/u/settings/api-keys"
    echo "2. Click 'Create API Key'"
    echo "3. Name it: 'Log Fetcher'"
    echo "4. Copy the key"
    echo ""
    echo "Opening API keys page..."
    open "https://dashboard.render.com/u/settings/api-keys" 2>/dev/null || true
    echo ""
    read -p "Paste your Render API key here: " RENDER_API_KEY
    
    if [ -z "$RENDER_API_KEY" ]; then
        echo "❌ No API key provided"
        exit 1
    fi
fi

echo "✅ API key found"
echo ""

# Find the service
echo "🔍 Finding ai-mate-api service..."
SERVICES=$(curl -s -H "Authorization: Bearer $RENDER_API_KEY" \
    "https://api.render.com/v1/services")

# Extract service ID for ai-mate-api
SERVICE_ID=$(echo "$SERVICES" | grep -B5 "ai-mate-api" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$SERVICE_ID" ]; then
    echo "❌ Could not find ai-mate-api service"
    echo "Available services:"
    echo "$SERVICES" | grep -o '"name":"[^"]*"' | cut -d'"' -f4
    exit 1
fi

echo "✅ Found service ID: $SERVICE_ID"
echo ""

# Get service details
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Service Status"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

SERVICE_DETAILS=$(curl -s -H "Authorization: Bearer $RENDER_API_KEY" \
    "https://api.render.com/v1/services/$SERVICE_ID")

# Extract status
STATUS=$(echo "$SERVICE_DETAILS" | grep -o '"serviceDetails":{"[^}]*"state":"[^"]*"' | grep -o '"state":"[^"]*"' | cut -d'"' -f4)
URL=$(echo "$SERVICE_DETAILS" | grep -o '"serviceDetails":{"[^}]*"url":"[^"]*"' | grep -o '"url":"[^"]*"' | cut -d'"' -f4)

echo "Status: $STATUS"
echo "URL: $URL"
echo ""

# Get latest deploy
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 Latest Deployment"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

DEPLOYS=$(curl -s -H "Authorization: Bearer $RENDER_API_KEY" \
    "https://api.render.com/v1/services/$SERVICE_ID/deploys?limit=1")

DEPLOY_ID=$(echo "$DEPLOYS" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
DEPLOY_STATUS=$(echo "$DEPLOYS" | grep -o '"status":"[^"]*"' | head -1 | cut -d'"' -f4)
CREATED_AT=$(echo "$DEPLOYS" | grep -o '"createdAt":"[^"]*"' | head -1 | cut -d'"' -f4)

echo "Deploy ID: $DEPLOY_ID"
echo "Status: $DEPLOY_STATUS"
echo "Created: $CREATED_AT"
echo ""

# Fetch logs
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📋 Recent Logs (Last 100 lines)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Note: Render's API doesn't have a direct logs endpoint
# We need to use the deploy logs endpoint
if [ -n "$DEPLOY_ID" ]; then
    echo "Fetching logs for deploy: $DEPLOY_ID"
    echo ""
    
    # Try to get logs via the service
    curl -s -H "Authorization: Bearer $RENDER_API_KEY" \
        "https://api.render.com/v1/services/$SERVICE_ID/deploys/$DEPLOY_ID" | \
        jq -r '.deploy.finishedAt, .deploy.status, .deploy.commitMessage' 2>/dev/null || echo "Could not parse deploy details"
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "💡 Note"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Render's API has limited log access."
echo "For detailed logs, use the dashboard:"
echo "https://dashboard.render.com/web/$SERVICE_ID/logs"
echo ""
echo "Opening dashboard logs..."
open "https://dashboard.render.com/web/$SERVICE_ID/logs" 2>/dev/null || true
echo ""

# Test the actual service
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🧪 Testing Service"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

if [ -n "$URL" ]; then
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$URL/api/health")
    echo "Health check: $URL/api/health"
    echo "HTTP Status: $HTTP_STATUS"
    echo ""
    
    if [ "$HTTP_STATUS" = "200" ]; then
        echo "✅ Service is LIVE and healthy!"
        echo ""
        curl -s "$URL/api/health" | jq '.' 2>/dev/null || curl -s "$URL/api/health"
    elif [ "$HTTP_STATUS" = "502" ]; then
        echo "⚠️  Service returning 502 Bad Gateway"
        echo "   Deploy Status: $DEPLOY_STATUS"
        echo ""
        if [ "$DEPLOY_STATUS" = "live" ]; then
            echo "   Service is marked as live but not responding."
            echo "   This usually means:"
            echo "   - App is crashing on startup"
            echo "   - Health check is failing"
            echo "   - Port binding issue"
        else
            echo "   Service is still deploying..."
            echo "   Current status: $DEPLOY_STATUS"
        fi
    else
        echo "⚠️  Unexpected HTTP status: $HTTP_STATUS"
    fi
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
