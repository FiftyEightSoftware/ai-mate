# 🚀 Cloud Deployment Steps - Execute These Commands

## Current Status
- ✅ Application built and ready
- ✅ Fly.io CLI installed
- ✅ Docker available
- ⏳ Authentication pending

---

## Option 1: Deploy to Fly.io (Recommended)

### Step 1: Authenticate with Fly.io
Open a new terminal and run:
```bash
/Users/nickphinesme.com/.fly/bin/flyctl auth login
```
This will open your browser for authentication.

### Step 2: Launch the Application
```bash
cd "/Users/nickphinesme.com/Developer/AI Mate"
/Users/nickphinesme.com/.fly/bin/flyctl launch --now --name ai-mate-app
```

### Step 3: Set Environment Variables
```bash
/Users/nickphinesme.com/.fly/bin/flyctl secrets set \
  AIMATE_DB_PASSWORD="$(openssl rand -base64 32)" \
  ASPNETCORE_ENVIRONMENT=Production
```

### Step 4: Create and Attach Redis (Optional but recommended)
```bash
/Users/nickphinesme.com/.fly/bin/flyctl redis create
# Follow prompts, then:
/Users/nickphinesme.com/.fly/bin/flyctl redis attach <your-redis-name>
```

### Step 5: Verify Deployment
```bash
/Users/nickphinesme.com/.fly/bin/flyctl open /api/health
```

---

## Option 2: Deploy to Railway

### Step 1: Authenticate with Railway
```bash
railway login
```

### Step 2: Initialize Project
```bash
cd "/Users/nickphinesme.com/Developer/AI Mate"
railway init
```

### Step 3: Add Redis Database
```bash
railway add -d redis
```

### Step 4: Deploy
```bash
railway up
```

### Step 5: Set Environment Variables
```bash
railway variables set AIMATE_DB_PASSWORD="$(openssl rand -base64 32)"
railway variables set ASPNETCORE_ENVIRONMENT=Production
railway variables set ASPNETCORE_URLS="http://0.0.0.0:8080"
```

### Step 6: Get Your URL
```bash
railway domain
```

---

## Option 3: GitHub + Render (Web UI - No CLI needed)

### Step 1: Push to GitHub
```bash
cd "/Users/nickphinesme.com/Developer/AI Mate"

# If you don't have a GitHub repo yet:
gh auth login
gh repo create ai-mate --public --source=. --push

# Or manually:
# 1. Create repo on github.com
# 2. git remote add origin https://github.com/YOUR-USERNAME/ai-mate.git
# 3. git push -u origin main
```

### Step 2: Deploy to Render
1. Go to https://render.com/
2. Sign in with GitHub
3. Click "New +" → "Blueprint"
4. Select your `ai-mate` repository
5. Render will automatically detect `render.yaml`
6. Click "Apply"
7. Wait 5-10 minutes for deployment

**That's it!** Render will:
- Deploy backend (with auto-generated database password)
- Create Redis instance
- Deploy frontend
- Connect everything automatically

---

## Option 4: Test Locally with Docker (Before Cloud Deployment)

### Step 1: Start Docker Desktop
```bash
open -a Docker
# Wait ~30 seconds for Docker to start
```

### Step 2: Build and Run
```bash
cd "/Users/nickphinesme.com/Developer/AI Mate"
docker-compose up -d
```

### Step 3: Check Status
```bash
docker-compose ps
docker-compose logs -f backend
```

### Step 4: Test the Application
- Frontend: http://localhost:5173
- Backend: http://localhost:5280
- Health: http://localhost:5280/api/health
- Metrics: http://localhost:5280/api/metrics

### Step 5: Stop Services
```bash
docker-compose down
```

---

## 🎯 My Recommendation

**For the fastest cloud deployment:**

1. **Use Fly.io** (if you want CLI control):
   ```bash
   /Users/nickphinesme.com/.fly/bin/flyctl auth login
   /Users/nickphinesme.com/.fly/bin/flyctl launch --now
   ```

2. **Use Render + GitHub** (if you prefer web UI):
   - Push to GitHub
   - Connect Render to your repo
   - One-click deploy with `render.yaml`

**Both options take ~10 minutes and include:**
- ✅ Backend API
- ✅ Frontend
- ✅ Redis cache
- ✅ Auto-scaling
- ✅ SSL certificates
- ✅ Health checks

---

## ✅ After Deployment - Verify

```bash
# Replace YOUR-APP-URL with your deployed URL

# Check health
curl https://YOUR-APP-URL.fly.dev/api/health | jq

# Expected output:
# {
#   "ok": true,
#   "status": "Healthy",
#   "time": "...",
#   "checks": [...]
# }

# Check metrics
curl https://YOUR-APP-URL.fly.dev/api/metrics | jq

# Test frontend
open https://YOUR-APP-URL.fly.dev
```

---

## 🆘 Troubleshooting

### "Docker daemon not running"
```bash
open -a Docker
sleep 30  # Wait for Docker to start
docker ps  # Verify it's running
```

### "Authentication failed"
Make sure you complete the browser authentication when prompted.

### "Build failed"
Check the logs:
```bash
# Fly.io
/Users/nickphinesme.com/.fly/bin/flyctl logs

# Railway
railway logs

# Docker
docker-compose logs
```

### "Can't access the app"
- Check if services are running: `docker-compose ps` or `flyctl status`
- Check logs for errors
- Verify health endpoint returns 200

---

## 📊 Next Steps After Deployment

1. **Add Custom Domain** (optional)
2. **Set up Monitoring** (Application Insights)
3. **Configure CI/CD** (GitHub Actions already configured)
4. **Scale Resources** (see SCALING.md)

---

**Choose your path and execute the commands above!**
The application is ready to deploy. 🚀
