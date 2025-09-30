#!/usr/bin/env bash
set -euo pipefail

# Deploy AI Mate to Fly.io
# Usage: ./scripts/deploy_flyio.sh

echo "🪂 Deploying AI Mate to Fly.io..."

# Check if Fly CLI is installed
if ! command -v fly &> /dev/null; then
    echo "❌ Fly CLI not found. Installing..."
    curl -L https://fly.io/install.sh | sh
    echo "⚠️  Please restart your terminal and re-run this script"
    exit 1
fi

# Login if not already
if ! fly auth whoami &> /dev/null; then
    echo "🔐 Please login to Fly.io..."
    fly auth login
fi

# Launch app (creates fly.toml if doesn't exist)
if [ ! -f "fly.toml" ]; then
    echo "📦 Creating new Fly.io app..."
    fly launch --no-deploy --config fly.toml
else
    echo "📦 Using existing fly.toml configuration..."
fi

# Create volume for database persistence
echo "💾 Creating persistent volume for database..."
fly volumes create aimate_data --size 1 --region lhr || echo "Volume already exists"

# Create Redis instance
echo "📦 Setting up Redis..."
fly redis create --name ai-mate-redis || echo "Redis already exists"

# Get Redis connection string
REDIS_URL=$(fly redis list --json | jq -r '.[0].PrivateUrl' || echo "")

# Set secrets
echo "🔧 Configuring secrets..."
read -p "Enter database password (or press Enter for auto-generated): " DB_PASSWORD
if [ -z "$DB_PASSWORD" ]; then
    DB_PASSWORD=$(openssl rand -base64 32)
    echo "Generated password: $DB_PASSWORD"
fi

fly secrets set AIMATE_DB_PASSWORD="$DB_PASSWORD"
fly secrets set ASPNETCORE_ENVIRONMENT="Production"

if [ -n "$REDIS_URL" ]; then
    fly secrets set REDIS_CONNECTION="$REDIS_URL"
fi

read -p "Enter frontend URL (e.g., https://ai-mate.pages.dev): " FRONTEND_URL
if [ -n "$FRONTEND_URL" ]; then
    fly secrets set FRONTEND_ORIGINS="$FRONTEND_URL"
fi

# Deploy
echo "🚀 Deploying to Fly.io..."
fly deploy

# Show app info
echo "✅ Deployment complete!"
echo ""
fly status
echo ""
echo "🌐 Your app URL:"
fly info --json | jq -r '.Hostname' | sed 's/^/https:\/\//'

echo ""
echo "📊 View logs: fly logs"
echo "🔍 SSH access: fly ssh console"
echo "📈 Metrics: fly dashboard"
