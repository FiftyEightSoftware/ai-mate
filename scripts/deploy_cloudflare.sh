#!/usr/bin/env bash
set -euo pipefail

# Deploy AI Mate frontend to Cloudflare Pages
# Usage: ./scripts/deploy_cloudflare.sh

echo "☁️  Deploying AI Mate to Cloudflare Pages..."

# Check if Wrangler is installed
if ! command -v wrangler &> /dev/null; then
    echo "❌ Wrangler not found. Installing..."
    npm install -g wrangler
fi

# Login if not already
if ! wrangler whoami &> /dev/null 2>&1; then
    echo "🔐 Please login to Cloudflare..."
    wrangler login
fi

# Build the frontend
echo "🔨 Building frontend..."
npm install
npm run build

# Get backend URL
read -p "Enter your backend API URL (e.g., https://ai-mate.fly.dev): " BACKEND_URL

if [ -z "$BACKEND_URL" ]; then
    echo "❌ Backend URL is required"
    exit 1
fi

# Create _redirects file for API proxy
echo "🔧 Configuring API proxy..."
cat > dist/_redirects << EOF
/api/* $BACKEND_URL/api/:splat 200
/voice/* $BACKEND_URL/voice/:splat 200
/health $BACKEND_URL/health 200
EOF

# Deploy to Cloudflare Pages
echo "🚀 Deploying to Cloudflare Pages..."
wrangler pages deploy dist --project-name=ai-mate

echo ""
echo "✅ Deployment complete!"
echo ""
echo "🌐 Your app will be available at:"
echo "   https://ai-mate.pages.dev"
echo ""
echo "📝 Next steps:"
echo "1. Update backend FRONTEND_ORIGINS to include: https://ai-mate.pages.dev"
echo "2. Set up custom domain (optional)"
echo "3. Configure Cloudflare Web Analytics"
echo "4. Enable Cloudflare DDoS protection"
echo ""
echo "🔧 Cloudflare Dashboard: https://dash.cloudflare.com"
