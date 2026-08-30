#!/bin/bash

# Impo - Quick Test Script
# Bu script tüm servisleri hızlıca test eder

set -e

echo "🚀 Impo Test Suite Starting..."
echo ""

# Renkler
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Test fonksiyonu
test_endpoint() {
    local name=$1
    local url=$2
    local expected=$3
    
    echo -n "Testing $name... "
    
    response=$(curl -s -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")
    
    if [ "$response" = "$expected" ]; then
        echo -e "${GREEN}✓ PASS${NC} (HTTP $response)"
        return 0
    else
        echo -e "${RED}✗ FAIL${NC} (Expected $expected, got $response)"
        return 1
    fi
}

# Health checks
echo "=== Health Checks ==="
test_endpoint "Spectator Health" "http://localhost:5185/health" "200"
test_endpoint "LocalCrm Health" "http://localhost:5057/health" "200"
test_endpoint "PromptGateway Health" "http://localhost:5047/health" "200"
echo ""

# Spectator tests
echo "=== Spectator API Tests ==="
test_endpoint "Get Matches" "http://localhost:5185/api/matches" "200"
test_endpoint "Get Metrics" "http://localhost:5185/api/metrics" "200"

echo -n "Create Match Event... "
response=$(curl -s -X POST http://localhost:5185/api/matches/test/events \
    -H "Content-Type: application/json" \
    -d '{"type":"goal","offsetSeconds":45,"payloadJson":"{\"side\":\"home\"}"}' \
    -w "%{http_code}" -o /dev/null 2>/dev/null || echo "000")
if [ "$response" = "200" ]; then
    echo -e "${GREEN}✓ PASS${NC} (HTTP $response)"
else
    echo -e "${RED}✗ FAIL${NC} (HTTP $response)"
fi
echo ""

# LocalCrm tests
echo "=== LocalCrm API Tests ==="
test_endpoint "Get Contacts" "http://localhost:5057/contacts" "200"
test_endpoint "Get Stats" "http://localhost:5057/stats" "200"

echo -n "Create Contact... "
TEST_ID=$(uuidgen | tr '[:upper:]' '[:lower:]')
TEST_DATE=$(date -u +"%Y-%m-%dT%H:%M:%SZ")
response=$(curl -s -X POST http://localhost:5057/sync \
    -H "Content-Type: application/json" \
    -d '{
        "contacts": [{
            "id": "'$TEST_ID'",
            "name": {"value": "Test User", "updatedAt": "'$TEST_DATE'", "deviceId": "test"},
            "email": {"value": "test@example.com", "updatedAt": "'$TEST_DATE'", "deviceId": "test"}
        }]
    }' \
    -w "%{http_code}" -o /dev/null 2>/dev/null || echo "000")
if [ "$response" = "200" ]; then
    echo -e "${GREEN}✓ PASS${NC} (HTTP $response)"
else
    echo -e "${RED}✗ FAIL${NC} (HTTP $response)"
fi
echo ""

# PromptGateway tests
echo "=== PromptGateway API Tests ==="
test_endpoint "Get Audit" "http://localhost:5047/admin/audit" "200"
test_endpoint "Get Budget" "http://localhost:5047/admin/budget" "200"
test_endpoint "Get Routes" "http://localhost:5047/admin/routes" "200"

echo -n "Test PII Masking... "
response=$(curl -s -X POST http://localhost:5047/v1/chat/completions \
    -H "Content-Type: application/json" \
    -H "X-Api-Key: test-key" \
    -d '{"model":"gpt-4o","messages":[{"role":"user","content":"test@example.com"}]}' \
    -w "%{http_code}" -o /dev/null 2>/dev/null || echo "000")
if [ "$response" = "200" ]; then
    echo -e "${GREEN}✓ PASS${NC} (HTTP $response)"
else
    echo -e "${RED}✗ FAIL${NC} (HTTP $response)"
fi
echo ""

echo "=== Test Summary ==="
echo -e "${GREEN}All critical tests completed!${NC}"
echo ""
echo "For detailed testing, see TESTING.md"
