#!/usr/bin/env bash
set -euo pipefail

echo "📦 Pushing AI Mate to GitHub"
echo "============================"
echo ""

# Check if git is initialized
if [ ! -d ".git" ]; then
    echo "Initializing git repository..."
    git init
    git add -A
    git commit -m "Initial commit: AI Mate with production infrastructure"
fi

# Check if gh CLI is available
if ! command -v gh &> /dev/null; then
    echo "GitHub CLI not found. Installing..."
    brew install gh
fi

echo ""
echo "Choose authentication method:"
echo "1. GitHub CLI (gh auth login) - Opens browser"
echo "2. Manual (you provide the repository URL)"
echo ""
read -p "Enter choice (1 or 2): " auth_choice

case $auth_choice in
    1)
        echo ""
        echo "Opening browser for GitHub authentication..."
        echo "Please complete the authentication in your browser."
        gh auth login
        
        echo ""
        read -p "Enter repository name (default: ai-mate): " repo_name
        repo_name=${repo_name:-ai-mate}
        
        echo ""
        read -p "Make repository public or private? (public/private, default: public): " visibility
        visibility=${visibility:-public}
        
        echo ""
        echo "Creating repository and pushing..."
        
        if gh repo create "$repo_name" --"$visibility" --source=. --remote=origin --push; then
            echo ""
            echo "✅ Repository created and code pushed!"
            echo ""
            repo_url=$(gh repo view --json url -q .url)
            echo "🌐 Repository URL: $repo_url"
            echo ""
            echo "📋 Next steps:"
            echo "1. Go to your repository: $repo_url"
            echo "2. Deploy to platform of choice:"
            echo "   - Render: https://render.com/"
            echo "   - Railway: https://railway.app/"
            echo "   - Vercel: https://vercel.com/"
            echo "   - Netlify: https://netlify.com/"
        else
            echo ""
            echo "⚠️  Repository might already exist. Pushing to existing repository..."
            git push -u origin main
        fi
        ;;
        
    2)
        echo ""
        echo "Manual GitHub setup:"
        echo ""
        echo "Step 1: Create a repository on GitHub:"
        echo "  Go to: https://github.com/new"
        echo ""
        read -p "Press Enter when you've created the repository..."
        echo ""
        read -p "Enter the repository URL (e.g., https://github.com/username/ai-mate.git): " repo_url
        
        if [ -z "$repo_url" ]; then
            echo "❌ Repository URL is required"
            exit 1
        fi
        
        echo ""
        echo "Adding remote and pushing..."
        
        # Remove origin if it exists
        git remote remove origin 2>/dev/null || true
        
        # Add new origin
        git remote add origin "$repo_url"
        
        # Push
        git branch -M main
        if git push -u origin main; then
            echo ""
            echo "✅ Code pushed successfully!"
            echo ""
            echo "🌐 Repository URL: $repo_url"
        else
            echo ""
            echo "❌ Push failed. You may need to authenticate:"
            echo ""
            echo "Option 1: Use HTTPS with Personal Access Token"
            echo "  1. Create token: https://github.com/settings/tokens/new"
            echo "  2. Use token as password when prompted"
            echo ""
            echo "Option 2: Use SSH"
            echo "  1. Generate SSH key: ssh-keygen -t ed25519"
            echo "  2. Add to GitHub: https://github.com/settings/keys"
            echo "  3. Change remote: git remote set-url origin git@github.com:username/ai-mate.git"
            echo ""
            exit 1
        fi
        ;;
        
    *)
        echo "❌ Invalid choice"
        exit 1
        ;;
esac

echo ""
echo "================================================"
echo "✅ GitHub setup complete!"
echo "================================================"
echo ""
echo "Your code is now on GitHub and ready for deployment!"
echo ""
echo "🚀 Quick Deploy Links:"
echo ""
echo "Render (Full Stack + Redis):"
echo "  https://dashboard.render.com/select-repo"
echo ""
echo "Railway (Full Stack):"
echo "  https://railway.app/new"
echo ""
echo "Vercel (Frontend):"
echo "  https://vercel.com/new"
echo ""
echo "Netlify (Frontend):"
echo "  https://app.netlify.com/start"
echo ""
echo "📚 See DEPLOY_NOW.md for detailed instructions"
echo ""
