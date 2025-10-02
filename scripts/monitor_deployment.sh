#!/bin/bash
# Monitor deployment status

BACKEND_URL="https://ai-mate-api.onrender.com/api/health"
FRONTEND_URL="https://68de77301b59cf59620c5593--ai-mate-1759409957.netlify.app"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔍 AI Mate Deployment Monitor"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Monitoring backend deployment..."
echo "Press Ctrl+C to stop"
echo ""

MAX_ATTEMPTS=60  # 10 minutes (60 * 10 seconds)
ATTEMPT=0

while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    ATTEMPT=$((ATTEMPT + 1))
    
    # Get HTTP status
    BACKEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$BACKEND_URL" 2>/dev/null)
    FRONTEND_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$FRONTEND_URL" 2>/dev/null)
    
    # Get current time
    TIMESTAMP=$(date "+%H:%M:%S")
    
    # Display status
    printf "[%s] Attempt %2d/%d | Backend: %s | Frontend: %s\n" \
        "$TIMESTAMP" "$ATTEMPT" "$MAX_ATTEMPTS" "$BACKEND_STATUS" "$FRONTEND_STATUS"
    
    # Check if backend is healthy
    if [ "$BACKEND_STATUS" = "200" ]; then
        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "✅ Backend is LIVE!"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        # Get health details
        echo "Health Check Response:"
        curl -s "$BACKEND_URL" | jq '.' 2>/dev/null || curl -s "$BACKEND_URL"
        echo ""
        
        # Test other endpoints
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🧪 Testing Additional Endpoints"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        
        echo "GET /api/dashboard:"
        DASH_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://ai-mate-api.onrender.com/api/dashboard")
        echo "  Status: $DASH_STATUS"
        
        echo "GET /api/invoices:"
        INV_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://ai-mate-api.onrender.com/api/invoices")
        echo "  Status: $INV_STATUS"
        
        echo "GET /api/jobs:"
        JOBS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://ai-mate-api.onrender.com/api/jobs")
        echo "  Status: $JOBS_STATUS"
        
        echo "GET /api/metrics:"
        METRICS_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "https://ai-mate-api.onrender.com/api/metrics")
        echo "  Status: $METRICS_STATUS"
        
        echo ""
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo "🌐 Live URLs"
        echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
        echo ""
        echo "Frontend: $FRONTEND_URL"
        echo "Backend:  https://ai-mate-api.onrender.com"
        echo ""
        echo "Opening frontend in browser..."
        open "$FRONTEND_URL" 2>/dev/null || true
        echo ""
        
        exit 0
    fi
    
    # Sleep before next check
    sleep 10
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "⚠️  Timeout reached"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Backend is still not responding after 10 minutes."
echo "Please check Render dashboard for build logs:"
echo "https://dashboard.render.com"
echo ""
