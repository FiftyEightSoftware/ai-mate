# AI Mate - Quick Start Guide

## 🚀 Deploy in 5 Minutes

### Option 1: Railway (Recommended for MVP)

```bash
./scripts/deploy_railway.sh
```

**What you get:**
- Backend API with Redis
- Auto-scaling
- Built-in SSL
- $5-20/month

**Time:** 5 minutes

---

### Option 2: Fly.io (Best Global Performance)

```bash
./scripts/deploy_flyio.sh
```

**What you get:**
- Multi-region deployment
- Redis included
- Edge network
- $20-50/month

**Time:** 7 minutes

---

### Option 3: Cloudflare Pages (Frontend Only)

```bash
npm install
npm run build
./scripts/deploy_cloudflare.sh
```

**What you get:**
- Free hosting
- Global CDN
- Unlimited bandwidth
- DDoS protection

**Time:** 3 minutes

---

## 🏃 Local Development

```bash
# Quick start (no Redis)
cd backend && dotnet run &
npm run dev

# Full stack with Redis
docker-compose up
```

**Access:**
- Frontend: http://localhost:5173
- Backend: http://localhost:5280
- Health: http://localhost:5280/api/health

---

## 🔧 Essential Configuration

```bash
# .env file (create this)
AIMATE_DB_PASSWORD=your-secure-password-here
REDIS_CONNECTION=redis://localhost:6379
FRONTEND_ORIGINS=http://localhost:5173
```

---

## 📊 Monitor Your App

```bash
# Health check
curl http://localhost:5280/api/health

# Performance metrics
curl http://localhost:5280/api/metrics

# View logs
tail -f backend/logs/ai-mate-*.log
```

---

## 🎯 Deployment Checklist

- [ ] Set strong `AIMATE_DB_PASSWORD`
- [ ] Configure `FRONTEND_ORIGINS`
- [ ] Set up Redis (optional but recommended)
- [ ] Enable Application Insights (optional)
- [ ] Test health endpoint
- [ ] Monitor metrics endpoint
- [ ] Set up alerts

---

## 💡 Pro Tips

1. **Use Redis from day 1** - Massive performance boost
2. **Monitor /api/metrics** - Catch issues early
3. **Start with Railway** - Easiest deployment
4. **Add Cloudflare** - Free CDN and DDoS protection
5. **Enable logging** - Essential for debugging

---

## 📚 Full Documentation

- **DEPLOYMENT.md** - Complete deployment guide
- **SCALING.md** - Scale from 0 to 1M+ users
- **IMPLEMENTATION_SUMMARY.md** - What's been built
- **README.md** - Project overview

---

## 🆘 Need Help?

**Common issues:**

```bash
# Port already in use
lsof -ti:5280 | xargs kill -9

# Can't connect to Redis
docker run -p 6379:6379 redis:7-alpine

# Build errors
dotnet restore backend/backend.csproj
dotnet build backend/backend.csproj
```

**Still stuck?**
- Check `/api/health` for system status
- Review logs in `logs/` directory
- See DEPLOYMENT.md for detailed troubleshooting

---

## ✅ What's Included

**Frontend (PWA)**
- Modern UI with toast notifications
- Voice assistant with Web Speech API
- Offline support
- Search and filtering
- Theme management
- Service Worker caching

**Backend (Production-Ready)**
- Redis caching
- Rate limiting (100 req/min)
- Health checks
- Performance monitoring
- Database migrations
- Structured logging

**DevOps**
- Docker Compose
- Multi-platform deployment
- CI/CD workflows
- Cost monitoring
- Auto-scaling configs

---

**Ready to launch!** Choose your platform and run the deploy script. 🚀
