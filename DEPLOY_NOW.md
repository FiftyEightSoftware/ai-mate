# 🚀 Deploy AI Mate Right Now

## Quickest Options (No Browser Authentication Needed)

### Option 1: Use Deployment Script (Automated)

```bash
./scripts/deploy_all.sh
```

This interactive script will guide you through all options!

---

## Manual Deployment (Step-by-Step)

### 🏃 Fastest: Render (One-Click Deploy)

**No CLI needed - Use web interface:**

1. Go to https://dashboard.render.com/select-repo
2. Connect your GitHub account (if not already)
3. Click "Deploy to Render" button
4. Render will auto-detect `render.yaml` and set everything up!

**OR push to GitHub first:**

```bash
# If you haven't pushed to GitHub yet
git remote add origin https://github.com/YOUR-USERNAME/ai-mate.git
git branch -M main
git push -u origin main
```

Then go to Render dashboard and select your repository.

---

### 🌐 Netlify (Frontend Only - Drag & Drop)

**Easiest way - No CLI:**

1. Go to https://app.netlify.com/drop
2. Drag the `dist/` folder to the page
3. Done! You'll get a URL like `https://random-name.netlify.app`

**With CLI (if authenticated):**

```bash
npm run build
cd dist
netlify deploy --prod
```

---

### 🚂 Railway (GitHub Integration)

**Via GitHub (Recommended):**

1. Push code to GitHub (see commands above)
2. Go to https://railway.app/new
3. Click "Deploy from GitHub repo"
4. Select `ai-mate`
5. Add Redis: Click "+ New" → "Database" → "Redis"
6. Done!

**With Token (No browser):**

```bash
# Get token from: https://railway.app/account/tokens
export RAILWAY_TOKEN="your-token-here"
railway up
```

---

### ✈️ Fly.io (Global Edge Network)

**With Token Auth:**

```bash
# Install Fly CLI
curl -L https://fly.io/install.sh | sh

# Get auth token from: https://fly.io/user/personal_access_tokens
fly auth token

# Deploy
fly launch --now
```

---

### 🐳 Docker Hub + Any Cloud

**Build and push Docker image:**

```bash
# Build
docker build -t your-username/ai-mate-backend:latest -f backend/Dockerfile .

# Login to Docker Hub
docker login

# Push
docker push your-username/ai-mate-backend:latest
```

Then deploy to:
- **AWS ECS**: Use the Docker image
- **Google Cloud Run**: `gcloud run deploy`
- **Azure Container Instances**: `az container create`
- **DigitalOcean Apps**: Use Docker Hub image

---

## 🎯 Recommended: GitHub-Based Deployment

This is the **easiest and most reliable** method:

### Step 1: Push to GitHub

```bash
# Create a new repository on GitHub.com first, then:

git remote add origin https://github.com/YOUR-USERNAME/ai-mate.git
git branch -M main
git push -u origin main
```

### Step 2: Choose Your Platform

Then deploy from GitHub to any of these (via web interface):

| Platform | Steps | Cost | Time |
|----------|-------|------|------|
| **Render** | Connect GitHub → Auto-deploy | Free tier | 3 min |
| **Railway** | Connect GitHub → Auto-deploy | $5/month | 3 min |
| **Vercel** | Connect GitHub → Auto-deploy | Free | 2 min |
| **Netlify** | Connect GitHub → Auto-deploy | Free | 2 min |

---

## 📱 Platform-Specific Instructions

### Render

1. Go to https://render.com/
2. Click "New +" → "Blueprint"
3. Connect your GitHub repository
4. Render reads `render.yaml` automatically
5. Click "Apply"
6. Wait 5-10 minutes for deployment

**Environment Variables Needed:**
- `AIMATE_DB_PASSWORD` - Auto-generated
- `REDIS_CONNECTION` - Auto-generated from Redis service

### Railway

1. Go to https://railway.app/
2. Click "New Project"
3. Choose "Deploy from GitHub repo"
4. Select `ai-mate`
5. Add Redis: "+ New" → "Database" → "Redis"
6. Set environment variables:
   - `AIMATE_DB_PASSWORD` - Generate a strong password
   - `REDIS_CONNECTION` - Copy from Redis service
   - `FRONTEND_ORIGINS` - Your frontend URL

### Vercel (Frontend)

1. Go to https://vercel.com/
2. Click "Add New..." → "Project"
3. Import your GitHub repository
4. Build settings:
   - Framework: Vite
   - Build Command: `npm run build`
   - Output Directory: `dist`
5. Deploy

### Netlify (Frontend)

1. Go to https://netlify.com/
2. Click "Add new site" → "Import an existing project"
3. Connect GitHub
4. Build settings:
   - Build command: `npm run build`
   - Publish directory: `dist`
5. Deploy

---

## 🔐 Environment Variables

Set these in your platform's dashboard:

**Required:**
```env
AIMATE_DB_PASSWORD=your-secure-password-here
ASPNETCORE_ENVIRONMENT=Production
FRONTEND_ORIGINS=https://your-frontend-url.com
```

**Optional (for better performance):**
```env
REDIS_CONNECTION=redis://...
ApplicationInsights__ConnectionString=...
```

---

## ✅ Verify Deployment

After deployment, test these endpoints:

```bash
# Health check
curl https://your-api-url.com/api/health

# Expected response:
# {"ok":true,"status":"Healthy","time":"...","checks":[...]}

# Metrics
curl https://your-api-url.com/api/metrics

# Frontend
# Open https://your-frontend-url.com in browser
```

---

## 🆘 Troubleshooting

### "Authentication failed"

**Solution:** Use GitHub-based deployment (web interface):
1. Push code to GitHub
2. Connect platform to your GitHub repository
3. Platform auto-deploys from git pushes

### "Build failed"

**Check:**
- `.NET 8 SDK` is specified in Dockerfile
- `Node.js 20` is specified for frontend
- All dependencies in `package.json` and `.csproj`

### "Can't connect to backend"

**Check:**
- `FRONTEND_ORIGINS` includes your frontend URL
- CORS is configured correctly
- Backend health endpoint returns 200

### "Database errors"

**Check:**
- `AIMATE_DB_PASSWORD` environment variable is set
- Database migrations ran successfully
- Check logs for specific error messages

---

## 📊 Monitoring After Deployment

### Check Application Health

```bash
# Detailed health check
curl https://your-api.com/api/health | jq

# Performance metrics
curl https://your-api.com/api/metrics | jq
```

### View Logs

**Render:**
```bash
# In dashboard, click on service → "Logs" tab
```

**Railway:**
```bash
railway logs
```

**Fly.io:**
```bash
fly logs
```

---

## 💰 Cost Comparison

| Platform | Free Tier | Paid Plan | Best For |
|----------|-----------|-----------|----------|
| **Render** | 750 hrs/month | $7/month | Easy deployment |
| **Railway** | $5 credit | $20/month | Full-stack apps |
| **Fly.io** | 3 VMs free | Usage-based | Global performance |
| **Vercel** | Unlimited | $20/month | Frontend |
| **Netlify** | 100GB/month | $19/month | Frontend |

---

## 🎯 My Top Recommendation

**For immediate deployment without CLI hassles:**

1. ✅ **Push to GitHub** (required once)
   ```bash
   git remote add origin https://github.com/YOUR-USERNAME/ai-mate.git
   git push -u origin main
   ```

2. ✅ **Deploy Backend to Render** (via web UI)
   - Go to https://render.com/
   - Connect GitHub repository
   - Uses `render.yaml` automatically
   - Includes Redis

3. ✅ **Deploy Frontend to Netlify** (via web UI)
   - Go to https://netlify.com/
   - Connect GitHub repository
   - Auto-deploys on git push

**Total time:** 10 minutes
**Cost:** Free tier (up to 750 hours/month)

---

## 📚 Additional Resources

- **DEPLOYMENT.md** - Comprehensive deployment guide
- **SCALING.md** - Scaling from 0 to 1M+ users
- **QUICK_START.md** - Quick reference
- **IMPLEMENTATION_SUMMARY.md** - What's been built

---

**Need help?** Check the logs and `/api/health` endpoint first!
