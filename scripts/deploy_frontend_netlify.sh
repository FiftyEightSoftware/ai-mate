#!/bin/bash
# AI Mate - Automated Netlify Frontend Deployment

set -e

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🚀 AI Mate - Netlify Frontend Deployment"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Check if Netlify CLI is installed
if ! command -v netlify &> /dev/null; then
    echo "📦 Installing Netlify CLI..."
    npm install -g netlify-cli
    echo "✅ Netlify CLI installed"
    echo ""
fi

# Build the frontend if needed
if [ ! -d "publish/wwwroot" ]; then
    echo "🔨 Building frontend..."
    cd ai_mate_blazor
    dotnet publish -c Release -o ../publish
    cd ..
    echo "✅ Build complete"
    echo ""
fi

echo "📁 Frontend build location: publish/wwwroot"
echo ""

# Check if already logged in
echo "🔐 Checking Netlify authentication..."
if netlify status 2>&1 | grep -q "Not logged in"; then
    echo "⚠️  Not logged in to Netlify"
    echo ""
    echo "Logging in to Netlify..."
    netlify login
    echo ""
fi

echo "✅ Authenticated with Netlify"
echo ""

# Deploy to Netlify
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📤 Deploying to Netlify..."
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

cd publish/wwwroot

# Deploy to Netlify (create new site if needed, then production deploy)
echo "Creating/deploying site..."
netlify deploy --prod --dir=. --open 2>&1 | tee /tmp/netlify-deploy.log

# Extract the URL from the output
FRONTEND_URL=$(grep -o 'https://[^[:space:]]*\.netlify\.app' /tmp/netlify-deploy.log | tail -1)

if [ -z "$FRONTEND_URL" ]; then
    # Try alternative URL format
    FRONTEND_URL=$(grep -o 'Website URL:.*' /tmp/netlify-deploy.log | sed 's/Website URL:[[:space:]]*//')
fi

cd ../..

echo ""
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "✅ Deployment Complete!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

if [ -n "$FRONTEND_URL" ]; then
    echo "🌐 Frontend URL: $FRONTEND_URL"
    echo ""
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo "📝 Next Step: Update Backend CORS"
    echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
    echo ""
    echo "In your Render dashboard:"
    echo "1. Go to: https://dashboard.render.com"
    echo "2. Click on 'ai-mate-api'"
    echo "3. Go to 'Environment' tab"
    echo "4. Update FRONTEND_ORIGINS to:"
    echo "   $FRONTEND_URL"
    echo "5. Save and redeploy"
    echo ""
    echo "Or run this command to save the URL:"
    echo "echo 'FRONTEND_URL=$FRONTEND_URL' >> .env.production"
    echo ""
else
    echo "⚠️  Could not extract URL automatically"
    echo "Check the Netlify dashboard: https://app.netlify.com/"
    echo ""
fi

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "🎉 Frontend is now live!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
