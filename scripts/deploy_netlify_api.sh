#!/bin/bash
# AI Mate - Netlify API Direct Upload Script
# This bypasses the CLI and uses the API directly

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 AI Mate - Netlify API Upload"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check for Netlify token
if [ -z "$NETLIFY_AUTH_TOKEN" ]; then
    echo "⚠️  NETLIFY_AUTH_TOKEN not found"
    echo ""
    echo "To get your token:"
    echo "1. Go to: https://app.netlify.com/user/applications#personal-access-tokens"
    echo "2. Click 'New access token'"
    echo "3. Name it: 'AI Mate Deploy'"
    echo "4. Copy the token"
    echo "5. Run: export NETLIFY_AUTH_TOKEN='your-token-here'"
    echo ""
    echo "Opening token page in 3 seconds..."
    sleep 3
    open "https://app.netlify.com/user/applications#personal-access-tokens" 2>/dev/null || true
    echo ""
    read -p "Paste your Netlify token here: " NETLIFY_AUTH_TOKEN
    
    if [ -z "$NETLIFY_AUTH_TOKEN" ]; then
        echo "❌ No token provided. Exiting."
        exit 1
    fi
    
    echo ""
    echo "💡 Tip: Save this token for future use:"
    echo "   echo 'export NETLIFY_AUTH_TOKEN=\"$NETLIFY_AUTH_TOKEN\"' >> ~/.zshrc"
    echo ""
fi

echo "✅ Netlify token found"
echo ""

# Ensure zip file exists
if [ ! -f "ai-mate-frontend.zip" ]; then
    echo "📦 Creating deployment package..."
    cd publish/wwwroot
    zip -q -r ../../ai-mate-frontend.zip .
    cd ../..
    echo "✅ Package created"
fi

echo "📁 Deployment package: ai-mate-frontend.zip"
echo "📊 Package size: $(du -h ai-mate-frontend.zip | cut -f1)"
echo ""

# Create a new site
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🏗️  Creating Netlify Site..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

SITE_RESPONSE=$(curl -s -X POST \
  "https://api.netlify.com/api/v1/sites" \
  -H "Authorization: Bearer $NETLIFY_AUTH_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "ai-mate-'$(date +%s)'",
    "custom_domain": null
  }')

# Check for errors
if echo "$SITE_RESPONSE" | grep -q '"code":'; then
    echo "❌ Failed to create site"
    echo "$SITE_RESPONSE" | grep -o '"message":"[^"]*"' || echo "$SITE_RESPONSE"
    exit 1
fi

SITE_ID=$(echo "$SITE_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
SITE_URL=$(echo "$SITE_RESPONSE" | grep -o '"url":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$SITE_ID" ]; then
    echo "❌ Could not extract site ID"
    echo "$SITE_RESPONSE"
    exit 1
fi

echo "✅ Site created!"
echo "   Site ID: $SITE_ID"
echo "   Site URL: $SITE_URL"
echo ""

# Deploy the site
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📤 Uploading to Netlify..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Deploy using zip file
DEPLOY_RESPONSE=$(curl -s -X POST \
  "https://api.netlify.com/api/v1/sites/${SITE_ID}/deploys" \
  -H "Authorization: Bearer $NETLIFY_AUTH_TOKEN" \
  -H "Content-Type: application/zip" \
  --data-binary "@ai-mate-frontend.zip")

# Check for errors
if echo "$DEPLOY_RESPONSE" | grep -q '"code":'; then
    echo "❌ Deployment failed"
    echo "$DEPLOY_RESPONSE" | grep -o '"message":"[^"]*"' || echo "$DEPLOY_RESPONSE"
    exit 1
fi

DEPLOY_ID=$(echo "$DEPLOY_RESPONSE" | grep -o '"id":"[^"]*"' | head -1 | cut -d'"' -f4)
DEPLOY_URL=$(echo "$DEPLOY_RESPONSE" | grep -o '"deploy_ssl_url":"[^"]*"' | head -1 | cut -d'"' -f4)

if [ -z "$DEPLOY_URL" ]; then
    DEPLOY_URL=$(echo "$DEPLOY_RESPONSE" | grep -o '"ssl_url":"[^"]*"' | head -1 | cut -d'"' -f4)
fi

if [ -z "$DEPLOY_URL" ]; then
    DEPLOY_URL=$SITE_URL
fi

echo "✅ Upload complete!"
echo "   Deploy ID: $DEPLOY_ID"
echo ""

# Wait for deploy to process
echo "⏳ Processing deployment..."
for i in {1..30}; do
    sleep 2
    DEPLOY_STATUS=$(curl -s -X GET \
      "https://api.netlify.com/api/v1/sites/${SITE_ID}/deploys/${DEPLOY_ID}" \
      -H "Authorization: Bearer $NETLIFY_AUTH_TOKEN")
    
    STATE=$(echo "$DEPLOY_STATUS" | grep -o '"state":"[^"]*"' | head -1 | cut -d'"' -f4)
    
    if [ "$STATE" = "ready" ]; then
        echo "✅ Deployment ready!"
        break
    elif [ "$STATE" = "error" ]; then
        echo "❌ Deployment failed"
        echo "$DEPLOY_STATUS" | grep -o '"error_message":"[^"]*"'
        exit 1
    fi
    
    echo "   Status: $STATE (${i}0s elapsed)"
done

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Deployment Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "🌐 Frontend URL: $DEPLOY_URL"
echo ""

# Save to file
echo "$DEPLOY_URL" > .netlify-url.txt
echo "📝 URL saved to: .netlify-url.txt"
echo ""

# Test the deployment
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🧪 Testing Deployment..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

HTTP_CODE=$(curl -s -o /dev/null -w "%{http_code}" "$DEPLOY_URL")

if [ "$HTTP_CODE" = "200" ]; then
    echo "✅ Frontend is live! (HTTP $HTTP_CODE)"
    echo ""
    echo "Opening in browser..."
    sleep 2
    open "$DEPLOY_URL" 2>/dev/null || true
else
    echo "⚠️  Frontend returned HTTP $HTTP_CODE"
    echo "   May still be initializing..."
fi

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📝 Next Steps"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "1. Update Backend CORS in Render:"
echo "   • Go to: https://dashboard.render.com"
echo "   • Click: ai-mate-api → Environment"
echo "   • Update FRONTEND_ORIGINS to: $DEPLOY_URL"
echo "   • Save and redeploy"
echo ""
echo "2. Test the full stack:"
echo "   • Frontend: $DEPLOY_URL"
echo "   • Backend:  https://ai-mate-api.onrender.com/api/health"
echo ""
echo "3. Configure custom domain (optional):"
echo "   • Netlify Dashboard: https://app.netlify.com/sites/$SITE_ID/settings/domain"
echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 All Done!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
