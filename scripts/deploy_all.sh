#!/usr/bin/env bash
set -euo pipefail

# Complete deployment script for AI Mate
# This script helps you deploy to multiple platforms

echo "🚀 AI Mate - Multi-Platform Deployment"
echo "======================================="
echo ""

# Function to check if command exists
command_exists() {
    command -v "$1" >/dev/null 2>&1
}

# Function to print colored output
print_step() {
    echo ""
    echo "🔹 $1"
    echo "-----------------------------------"
}

print_success() {
    echo "✅ $1"
}

print_error() {
    echo "❌ $1"
}

# Check prerequisites
print_step "Checking prerequisites"

if ! command_exists git; then
    print_error "Git not found. Please install git."
    exit 1
fi
print_success "Git installed"

if ! command_exists npm; then
    print_error "npm not found. Please install Node.js."
    exit 1
fi
print_success "npm installed"

if ! command_exists dotnet; then
    print_error ".NET SDK not found. Please install .NET 8 SDK."
    exit 1
fi
print_success ".NET SDK installed"

# Build the application
print_step "Building application"

echo "Building frontend..."
npm install
npm run build
print_success "Frontend built"

echo "Building backend..."
cd backend
dotnet restore
dotnet build -c Release
cd ..
print_success "Backend built"

# Show deployment options
print_step "Deployment Options"

echo "Choose your deployment platform:"
echo ""
echo "1. GitHub + Railway (Recommended - Auto-deploy on git push)"
echo "2. Netlify (Frontend only - Free)"
echo "3. Fly.io (Full stack - Global edge network)"
echo "4. Render (Full stack - Free tier available)"
echo "5. Docker Compose (Local production test)"
echo "6. All of the above!"
echo ""

read -p "Enter your choice (1-6): " choice

case $choice in
    1)
        print_step "GitHub + Railway Deployment"
        
        if ! command_exists gh; then
            echo "Installing GitHub CLI..."
            brew install gh
        fi
        
        echo "Please authenticate with GitHub:"
        gh auth login
        
        echo ""
        echo "Creating GitHub repository..."
        gh repo create ai-mate --public --source=. --push || echo "Repository may already exist"
        
        print_success "Code pushed to GitHub"
        
        echo ""
        echo "📋 Next steps:"
        echo "1. Go to https://railway.app"
        echo "2. Click 'New Project'"
        echo "3. Select 'Deploy from GitHub repo'"
        echo "4. Choose 'ai-mate' repository"
        echo "5. Railway will auto-deploy!"
        echo ""
        echo "💡 Add Redis: In Railway dashboard, click '+ New' → 'Database' → 'Redis'"
        ;;
        
    2)
        print_step "Netlify Deployment"
        
        if ! command_exists netlify; then
            echo "Installing Netlify CLI..."
            npm install -g netlify-cli
        fi
        
        echo "Logging in to Netlify..."
        netlify login
        
        echo "Deploying to Netlify..."
        netlify deploy --prod --dir=dist
        
        print_success "Deployed to Netlify"
        ;;
        
    3)
        print_step "Fly.io Deployment"
        
        if ! command_exists fly; then
            echo "Installing Fly CLI..."
            curl -L https://fly.io/install.sh | sh
            export FLYCTL_INSTALL="$HOME/.fly"
            export PATH="$FLYCTL_INSTALL/bin:$PATH"
        fi
        
        ./scripts/deploy_flyio.sh
        ;;
        
    4)
        print_step "Render Deployment"
        
        echo "📋 Render Deployment Steps:"
        echo ""
        echo "Backend:"
        echo "1. Go to https://render.com"
        echo "2. Click 'New +' → 'Web Service'"
        echo "3. Connect your GitHub repository"
        echo "4. Settings:"
        echo "   - Name: ai-mate-api"
        echo "   - Environment: Docker"
        echo "   - Dockerfile path: backend/Dockerfile"
        echo "   - Add Redis: Click 'New +' → 'Redis'"
        echo ""
        echo "Frontend:"
        echo "1. Click 'New +' → 'Static Site'"
        echo "2. Connect your GitHub repository"
        echo "3. Settings:"
        echo "   - Build command: npm run build"
        echo "   - Publish directory: dist"
        ;;
        
    5)
        print_step "Docker Compose (Local Production Test)"
        
        if ! command_exists docker; then
            print_error "Docker not found. Please install Docker Desktop."
            exit 1
        fi
        
        echo "Starting services..."
        docker-compose up -d
        
        echo ""
        print_success "Services started"
        echo ""
        echo "🌐 Access your app:"
        echo "   Frontend: http://localhost:5173"
        echo "   Backend: http://localhost:5280"
        echo "   Health: http://localhost:5280/api/health"
        echo ""
        echo "📊 View logs:"
        echo "   docker-compose logs -f"
        echo ""
        echo "🛑 Stop services:"
        echo "   docker-compose down"
        ;;
        
    6)
        print_step "Comprehensive Deployment Setup"
        
        echo "This will prepare your app for deployment to all platforms!"
        echo ""
        
        # GitHub
        if ! command_exists gh; then
            brew install gh
        fi
        echo "✓ GitHub CLI ready"
        
        # Netlify
        if ! command_exists netlify; then
            npm install -g netlify-cli
        fi
        echo "✓ Netlify CLI ready"
        
        # Railway
        if ! command_exists railway; then
            npm install -g @railway/cli
        fi
        echo "✓ Railway CLI ready"
        
        # Fly
        if ! command_exists fly; then
            curl -L https://fly.io/install.sh | sh
        fi
        echo "✓ Fly CLI ready"
        
        print_success "All deployment tools installed"
        
        echo ""
        echo "📋 Quick Deployment Commands:"
        echo ""
        echo "GitHub:"
        echo "  gh auth login && gh repo create ai-mate --public --source=. --push"
        echo ""
        echo "Netlify:"
        echo "  netlify login && netlify deploy --prod --dir=dist"
        echo ""
        echo "Railway:"
        echo "  railway login --browserless && railway up"
        echo ""
        echo "Fly.io:"
        echo "  fly auth login && fly launch"
        echo ""
        echo "Docker:"
        echo "  docker-compose up"
        ;;
        
    *)
        print_error "Invalid choice"
        exit 1
        ;;
esac

echo ""
echo "================================================"
print_success "Deployment process complete!"
echo "================================================"
echo ""
echo "📚 For more details, see:"
echo "   - DEPLOYMENT.md - Complete deployment guide"
echo "   - SCALING.md - Scaling strategies"
echo "   - QUICK_START.md - Quick reference"
echo ""
