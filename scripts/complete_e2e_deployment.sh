#!/bin/bash
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Configuration
BACKEND_URL="https://ai-mate-api.onrender.com"
FRONTEND_LOCAL="http://localhost:8080"
LOG_FILE="/tmp/e2e_deployment_$(date +%Y%m%d_%H%M%S).log"

# Track results
TOTAL_TESTS=0
PASSED_TESTS=0
FAILED_TESTS=0

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 AI MATE - COMPLETE END-TO-END DEPLOYMENT & TEST SUITE"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Started: $(date)"
echo "Log file: $LOG_FILE"
echo ""

# ============================================================================
# PHASE 1: BACKEND VERIFICATION
# ============================================================================
echo -e "${BLUE}━━━ PHASE 1: Backend Verification ━━━${NC}"
echo ""

echo "1.1 Checking backend health..."
if curl -s "$BACKEND_URL/api/health" | grep -q "ok"; then
    echo -e "  ${GREEN}✓${NC} Backend is live"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Backend health check failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

echo ""
echo "1.2 Verifying backend endpoints..."
DASHBOARD=$(curl -s "$BACKEND_URL/api/dashboard")
if echo "$DASHBOARD" | python3 -c "import json, sys; d=json.load(sys.stdin); assert 'outstandingTotal' in d" 2>/dev/null; then
    echo -e "  ${GREEN}✓${NC} Dashboard endpoint working"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Dashboard endpoint failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

# ============================================================================
# PHASE 2: BACKEND BUILD & TESTS
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 2: Backend Build & Tests ━━━${NC}"
echo ""

echo "2.1 Building backend..."
cd backend
if dotnet build -c Release --no-restore > /dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Backend build successful"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Backend build failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))
cd ..

# ============================================================================
# PHASE 3: FRONTEND BUILD & UNIT TESTS
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 3: Frontend Build & Unit Tests ━━━${NC}"
echo ""

echo "3.1 Building frontend..."
cd ai_mate_blazor
if dotnet build -c Release > /dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Frontend build successful"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Frontend build failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))
cd ..

echo ""
echo "3.2 Running frontend unit tests..."
cd ai_mate_blazor.Tests
TEST_RESULT=$(dotnet test --verbosity quiet 2>&1)
if echo "$TEST_RESULT" | grep -q "Passed!"; then
    TEST_COUNT=$(echo "$TEST_RESULT" | grep -oE "Passed:    [0-9]+" | grep -oE "[0-9]+")
    echo -e "  ${GREEN}✓${NC} All $TEST_COUNT unit tests passed"
    ((PASSED_TESTS+=TEST_COUNT))
    ((TOTAL_TESTS+=TEST_COUNT))
else
    echo -e "  ${RED}✗${NC} Some unit tests failed"
    echo "$TEST_RESULT" | grep -E "(Failed|Error)"
    ((FAILED_TESTS++))
    ((TOTAL_TESTS++))
fi
cd ..

# ============================================================================
# PHASE 4: INTEGRATION TESTS
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 4: Integration Tests ━━━${NC}"
echo ""

echo "4.1 Testing API endpoints with live data..."

echo "  - GET /api/invoices"
INVOICES=$(curl -s "$BACKEND_URL/api/invoices" | python3 -c "import json, sys; print(len(json.load(sys.stdin)))" 2>/dev/null)
if [ ! -z "$INVOICES" ]; then
    echo -e "    ${GREEN}✓${NC} Retrieved $INVOICES invoices"
    ((PASSED_TESTS++))
else
    echo -e "    ${RED}✗${NC} Failed to get invoices"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

echo "  - GET /api/jobs"
JOBS=$(curl -s "$BACKEND_URL/api/jobs" | python3 -c "import json, sys; print(len(json.load(sys.stdin)))" 2>/dev/null)
if [ ! -z "$JOBS" ]; then
    echo -e "    ${GREEN}✓${NC} Retrieved $JOBS jobs"
    ((PASSED_TESTS++))
else
    echo -e "    ${RED}✗${NC} Failed to get jobs"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

echo "  - POST /api/jobs (create)"
NEW_JOB=$(curl -s -X POST "$BACKEND_URL/api/jobs" \
    -H "Content-Type: application/json" \
    -d '{"title":"E2E Test Job","status":"Upcoming","quotedPrice":1500.00}' \
    | python3 -c "import json, sys; print(json.load(sys.stdin).get('id', ''))" 2>/dev/null)
if [ ! -z "$NEW_JOB" ] && [ "$NEW_JOB" != "None" ]; then
    echo -e "    ${GREEN}✓${NC} Created job: $NEW_JOB"
    ((PASSED_TESTS++))
else
    echo -e "    ${RED}✗${NC} Failed to create job"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

# ============================================================================
# PHASE 5: FRONTEND DEPLOYMENT
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 5: Frontend Deployment ━━━${NC}"
echo ""

echo "5.1 Publishing frontend..."
cd ai_mate_blazor
if dotnet publish -c Release -o ../publish-frontend > /dev/null 2>&1; then
    echo -e "  ${GREEN}✓${NC} Frontend published"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Frontend publish failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))
cd ..

echo ""
echo "5.2 Configuring frontend for production..."
if [ -f "publish-frontend/wwwroot/appsettings.json" ]; then
    API_BASE=$(cat publish-frontend/wwwroot/appsettings.json | python3 -c "import json, sys; print(json.load(sys.stdin).get('API_BASE', ''))")
    if [ "$API_BASE" = "$BACKEND_URL" ]; then
        echo -e "  ${GREEN}✓${NC} API configuration correct: $API_BASE"
        ((PASSED_TESTS++))
    else
        echo -e "  ${YELLOW}⚠${NC} API_BASE is $API_BASE (expected $BACKEND_URL)"
        ((FAILED_TESTS++))
    fi
else
    echo -e "  ${RED}✗${NC} appsettings.json not found"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

# ============================================================================
# PHASE 6: END-TO-END SMOKE TESTS
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 6: End-to-End Smoke Tests ━━━${NC}"
echo ""

echo "6.1 Starting local frontend server..."
cd publish-frontend/wwwroot
python3 -m http.server 8080 > /dev/null 2>&1 &
FRONTEND_PID=$!
sleep 3

if curl -s "$FRONTEND_LOCAL" | grep -q "AI-Mate"; then
    echo -e "  ${GREEN}✓${NC} Frontend server running"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Frontend server failed"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))
cd ../..

echo ""
echo "6.2 Testing frontend resources..."
if curl -s "$FRONTEND_LOCAL" | grep -q "blazor.webassembly.js"; then
    echo -e "  ${GREEN}✓${NC} Blazor WebAssembly loaded"
    ((PASSED_TESTS++))
else
    echo -e "  ${RED}✗${NC} Blazor not detected"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

# ============================================================================
# PHASE 7: iOS SIMULATOR
# ============================================================================
echo ""
echo -e "${BLUE}━━━ PHASE 7: iOS Simulator Launch ━━━${NC}"
echo ""

echo "7.1 Booting iOS Simulator..."
if xcrun simctl boot "iPhone 16 Pro" 2>/dev/null || xcrun simctl list devices booted | grep -q "Booted"; then
    echo -e "  ${GREEN}✓${NC} iOS Simulator booted"
    ((PASSED_TESTS++))
    
    sleep 2
    open -a Simulator 2>/dev/null
    sleep 2
    
    echo "7.2 Opening frontend in simulator..."
    if xcrun simctl openurl booted "$FRONTEND_LOCAL" 2>/dev/null; then
        echo -e "  ${GREEN}✓${NC} Frontend opened in simulator"
        ((PASSED_TESTS++))
    else
        echo -e "  ${YELLOW}⚠${NC} Could not open URL in simulator"
        ((FAILED_TESTS++))
    fi
    ((TOTAL_TESTS++))
else
    echo -e "  ${RED}✗${NC} Failed to boot simulator"
    ((FAILED_TESTS++))
fi
((TOTAL_TESTS++))

# ============================================================================
# FINAL RESULTS
# ============================================================================
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 FINAL RESULTS"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "Total Tests:  $TOTAL_TESTS"
echo -e "Passed:       ${GREEN}$PASSED_TESTS${NC}"
echo -e "Failed:       ${RED}$FAILED_TESTS${NC}"
echo ""

if [ $FAILED_TESTS -eq 0 ]; then
    echo -e "${GREEN}✅ ALL TESTS PASSED - DEPLOYMENT SUCCESSFUL!${NC}"
    EXIT_CODE=0
else
    PASS_RATE=$((PASSED_TESTS * 100 / TOTAL_TESTS))
    echo -e "${YELLOW}⚠ DEPLOYMENT COMPLETED WITH $FAILED_TESTS FAILURES ($PASS_RATE% pass rate)${NC}"
    EXIT_CODE=1
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🔗 DEPLOYMENT URLS"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "Backend:  $BACKEND_URL"
echo "Frontend: $FRONTEND_LOCAL"
echo "iOS Simulator: Running"
echo ""
echo "Completed: $(date)"
echo ""
echo "Frontend server PID: $FRONTEND_PID (stop with: kill $FRONTEND_PID)"
echo ""

exit $EXIT_CODE
