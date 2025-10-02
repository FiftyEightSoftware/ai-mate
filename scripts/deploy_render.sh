#!/bin/bash
# AI Mate - Automated Render Deployment Script
# This script automates the Render deployment process as much as possible

set -e

REPO_URL="https://github.com/FiftyEightSoftware/ai-mate"
RENDER_DASHBOARD="https://dashboard.render.com"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 AI Mate - Render Deployment Script"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check if Render API key is set
if [ -z "$RENDER_API_KEY" ]; then
    echo "⚠️  RENDER_API_KEY not found"
    echo ""
    echo "To automate deployment, you need a Render API key:"
    echo "1. Go to: https://dashboard.render.com/u/settings#api-keys"
    echo "2. Create a new API key"
    echo "3. Export it: export RENDER_API_KEY='your-key-here'"
    echo ""
    echo "OR continue with web-based deployment..."
    echo ""
    read -p "Continue with web deployment? (y/n): " -n 1 -r
    echo
    if [[ ! $REPLY =~ ^[Yy]$ ]]; then
        exit 1
    fi
    
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "📋 Web-Based Deployment Steps"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "✅ Step 1: Your code is on GitHub"
    echo "   Repository: $REPO_URL"
    echo ""
    echo "✅ Step 2: Open Render Dashboard"
    echo "   Opening in 3 seconds..."
    sleep 3
    open "$RENDER_DASHBOARD/select-repo?type=blueprint" 2>/dev/null || echo "   Go to: $RENDER_DASHBOARD/select-repo?type=blueprint"
    echo ""
    echo "📝 Follow these steps in your browser:"
    echo ""
    echo "   1. Sign up/Login (use GitHub account)"
    echo "   2. Click 'Connect' next to GitHub"
    echo "   3. Authorize Render to access your repos"
    echo "   4. Select: FiftyEightSoftware/ai-mate"
    echo "   5. Render will detect render.yaml automatically"
    echo "   6. Review the 3 services:"
    echo "      • ai-mate-api (Backend)"
    echo "      • ai-mate-redis (Cache)"
    echo "      • ai-mate-frontend (Frontend)"
    echo "   7. Click 'Apply' to deploy"
    echo "   8. Wait 5-10 minutes for deployment"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "💡 After deployment, you'll get URLs like:"
    echo "   • Backend:  https://ai-mate-api.onrender.com"
    echo "   • Frontend: https://ai-mate-frontend.onrender.com"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    exit 0
fi

echo "✅ RENDER_API_KEY found"
echo ""

# Function to make Render API calls
render_api() {
    local endpoint=$1
    local method=${2:-GET}
    local data=${3:-}
    
    if [ -n "$data" ]; then
        curl -s -X "$method" \
            -H "Authorization: Bearer $RENDER_API_KEY" \
            -H "Content-Type: application/json" \
            -d "$data" \
            "https://api.render.com/v1${endpoint}"
    else
        curl -s -X "$method" \
            -H "Authorization: Bearer $RENDER_API_KEY" \
            "https://api.render.com/v1${endpoint}"
    fi
}

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔍 Checking Render Account"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Test API connection and get account info
ACCOUNT_INFO=$(render_api "/owners" GET)

if echo "$ACCOUNT_INFO" | grep -q "error"; then
    echo "❌ Failed to connect to Render API"
    echo "$ACCOUNT_INFO"
    exit 1
fi

echo "✅ Connected to Render API"
echo ""

# Get owner ID
OWNER_ID=$(echo "$ACCOUNT_INFO" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$OWNER_ID" ]; then
    echo "❌ Could not find owner ID"
    exit 1
fi

echo "✅ Owner ID: $OWNER_ID"
echo ""

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📦 Deploying Services"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Note: Render's API doesn't directly support Blueprint deployments
# We need to create services individually or use the web interface

echo "⚠️  Note: Render's API doesn't fully support Blueprint deployments"
echo "   The render.yaml file is best deployed via the web interface."
echo ""
echo "   However, I can help you verify the deployment after you create it."
echo ""

read -p "Have you already deployed via Render dashboard? (y/n): " -n 1 -r
echo
echo ""

if [[ $REPLY =~ ^[Yy]$ ]]; then
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "🔍 Checking Deployment Status"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    
    # List all services
    SERVICES=$(render_api "/owners/$OWNER_ID/services" GET)
    
    # Check for ai-mate services
    if echo "$SERVICES" | grep -q "ai-mate"; then
        echo "✅ Found AI Mate services:"
        echo ""
        
        # Parse and display service info
        echo "$SERVICES" | grep -o '"name":"[^"]*"' | cut -d'"' -f4 | grep "ai-mate" | while read service; do
            echo "   • $service"
        done
        echo ""
        
        # Get service URLs
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🌐 Service URLs"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        # Extract service IDs and get details
        SERVICE_IDS=$(echo "$SERVICES" | grep -B5 "ai-mate" | grep -o '"id":"[^"]*"' | cut -d'"' -f4)
        
        for service_id in $SERVICE_IDS; do
            SERVICE_DETAIL=$(render_api "/services/$service_id" GET)
            SERVICE_NAME=$(echo "$SERVICE_DETAIL" | grep -o '"name":"[^"]*"' | head -1 | cut -d'"' -f4)
            SERVICE_URL=$(echo "$SERVICE_DETAIL" | grep -o '"url":"[^"]*"' | head -1 | cut -d'"' -f4)
            
            if [ -n "$SERVICE_URL" ]; then
                echo "   $SERVICE_NAME:"
                echo "   → $SERVICE_URL"
                echo ""
            fi
        done
        
        # Test backend health
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🏥 Testing Backend Health"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        BACKEND_URL=$(echo "$SERVICES" | grep -B10 "ai-mate-api" | grep -o '"url":"[^"]*"' | head -1 | cut -d'"' -f4)
        
        if [ -n "$BACKEND_URL" ]; then
            echo "Testing: $BACKEND_URL/api/health"
            HEALTH=$(curl -s "$BACKEND_URL/api/health" || echo "failed")
            
            if echo "$HEALTH" | grep -q "Healthy"; then
                echo "✅ Backend is healthy!"
                echo "$HEALTH" | grep -o '"status":"[^"]*"' || true
            else
                echo "⚠️  Backend may still be starting up..."
                echo "   Check: $BACKEND_URL/api/health"
            fi
        fi
        
        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "✅ Deployment Check Complete!"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    else
        echo "⚠️  No AI Mate services found yet"
        echo "   Please deploy via Render dashboard first"
        echo ""
        open "$RENDER_DASHBOARD/select-repo?type=blueprint" 2>/dev/null || true
    fi
else
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "📋 Next Steps"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "1. Open Render Dashboard"
    echo "   → $RENDER_DASHBOARD/select-repo?type=blueprint"
    echo ""
    echo "2. Connect your GitHub repository"
    echo "   → Select: FiftyEightSoftware/ai-mate"
    echo ""
    echo "3. Apply the Blueprint"
    echo "   → Render will use your render.yaml"
    echo ""
    echo "4. Run this script again to verify deployment"
    echo "   → ./scripts/deploy_render.sh"
    echo ""
    
    open "$RENDER_DASHBOARD/select-repo?type=blueprint" 2>/dev/null || true
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📚 Resources"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "• Render Dashboard:    $RENDER_DASHBOARD"
echo "• GitHub Repository:   $REPO_URL"
echo "• Deployment Guide:    deploy-to-render.md"
echo "• Test Report:         ONBOARDING_TEST_REPORT.md"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
