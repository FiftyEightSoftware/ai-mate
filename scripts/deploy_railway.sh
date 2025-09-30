#!/usr/bin/env bash
set -euo pipefail

# Deploy AI Mate to Railway
# Usage: ./scripts/deploy_railway.sh

echo "🚂 Deploying AI Mate to Railway..."

# Check if Railway CLI is installed
if ! command -v railway &> /dev/null; then
    echo "❌ Railway CLI not found. Installing..."
    npm install -g @railway/cli
fi

# Login if not already
if ! railway whoami &> /dev/null; then
    echo "🔐 Please login to Railway..."
    railway login
fi

# Initialize project if needed
if [ ! -f "railway.json" ]; then
    echo "📦 Initializing Railway project..."
    railway init
fi

# Add Redis if not exists
echo "📦 Setting up Redis..."
railway add redis || echo "Redis already exists"

# Set environment variables
echo "🔧 Configuring environment variables..."
read -p "Enter database password (or press Enter for auto-generated): " DB_PASSWORD
if [ -z "$DB_PASSWORD" ]; then
    DB_PASSWORD=$(openssl rand -base64 32)
    echo "Generated password: $DB_PASSWORD"
fi

railway variables set AIMATE_DB_PASSWORD="$DB_PASSWORD"
railway variables set ASPNETCORE_ENVIRONMENT="Production"
railway variables set FRONTEND_ORIGINS="https://your-frontend-url.com"

# Deploy
echo "🚀 Deploying..."
railway up

# Get the deployment URL
echo "✅ Deployment complete!"
echo "🌐 Your API URL:"
railway domain

echo ""
echo "Next steps:"
echo "1. Note the API URL above"
echo "2. Deploy frontend to Cloudflare Pages"
echo "3. Update FRONTEND_ORIGINS with actual frontend URL"
echo "4. Set REDIS_CONNECTION if needed"
