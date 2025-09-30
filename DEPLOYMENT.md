# AI Mate Deployment Guide

Complete guide for deploying AI Mate to production with global scale support.

## Table of Contents
1. [Quick Start](#quick-start)
2. [Azure Deployment](#azure-deployment)
3. [Railway Deployment](#railway-deployment)
4. [Fly.io Deployment](#flyio-deployment)
5. [Cloudflare Pages](#cloudflare-pages)
6. [Environment Variables](#environment-variables)
7. [Monitoring & Observability](#monitoring--observability)
8. [Cost Optimization](#cost-optimization)

---

## Quick Start

### Local Development with Docker Compose

```bash
# Start all services (backend + Redis + frontend)
docker-compose up

# Access:
# Frontend: http://localhost:5173
# Backend: http://localhost:5280
# Redis: localhost:6379
```

### Local Development (Manual)

```bash
# Terminal 1: Start Redis (optional)
docker run -p 6379:6379 redis:7-alpine

# Terminal 2: Start Backend
cd backend
dotnet run --urls "http://0.0.0.0:5280"

# Terminal 3: Start Frontend
npm run dev
```

---

## Azure Deployment

### Prerequisites
- Azure CLI installed and logged in
- GitHub repository set up
- Azure subscription with billing configured

### 1. Fix Azure Subscription (If Needed)

If you encounter subscription access issues:

```bash
# Check subscription status
az account list --all

# Contact Azure support or check:
# - Billing is set up
# - Subscription is active (not disabled)
# - You have Contributor/Owner role
```

###2. Create Resources

```bash
# Set variables
RG="rg-ai-mate"
LOCATION="westeurope"
APP_NAME="ai-mate-api"

# Login and set subscription
az login
az account set --subscription "<YOUR_SUBSCRIPTION_ID>"

# Create resource group
az group create -n $RG -l $LOCATION

# Create storage account
SA_NAME="aimatestorage$(date +%s)"
az storage account create -g $RG -n $SA_NAME -l $LOCATION --sku Standard_LRS

# Create file share for database
az storage share-rm create \
  --resource-group $RG \
  --storage-account $SA_NAME \
  --name aimate-db \
  --quota 5

# Create Redis Cache
az redis create \
  --name ai-mate-redis \
  --resource-group $RG \
  --location $LOCATION \
  --sku Basic \
  --vm-size c0

# Create Container Apps environment
az containerapp env create \
  --name cae-aimate \
  --resource-group $RG \
  --location $LOCATION

# Create Application Insights
az monitor app-insights component create \
  --app ai-mate-insights \
  --location $LOCATION \
  --resource-group $RG
```

### 3. Configure GitHub Secrets

Add these secrets to your GitHub repository (Settings → Secrets):

- `AZURE_CLIENT_ID` - Service principal client ID
- `AZURE_TENANT_ID` - Azure AD tenant ID
- `AZURE_SUBSCRIPTION_ID` - Your subscription ID
- `AIMATE_DB_PASSWORD` - Strong database encryption password
- `AZURE_STATIC_WEB_APPS_API_TOKEN` - From Static Web App resource
- `BACKEND_URL` - Will be the Container App URL after deployment

### 4. Deploy Backend

Push to main branch or run the GitHub Actions workflow manually:

```bash
git add -A
git commit -m "Deploy to Azure"
git push origin main
```

Then run the `backend-aca-deploy` workflow with:
- image_ref: `ghcr.io/<your-username>/ai-mate/backend:latest`
- resource_group: `rg-ai-mate`
- containerapp_name: `ai-mate-api`
- env_FRONTEND_ORIGINS: Your frontend URL

### 5. Deploy Frontend

The `frontend-swa-deploy` workflow will automatically deploy on push to main.

---

## Railway Deployment

### Quick Deploy

1. Install Railway CLI:
```bash
npm i -g @railway/cli
```

2. Login and create project:
```bash
railway login
railway init
```

3. Add services:
```bash
# Add Redis
railway add redis

# Deploy backend
railway up
```

4. Set environment variables:
```bash
railway variables set AIMATE_DB_PASSWORD="your-secure-password"
railway variables set REDIS_CONNECTION="redis://default:password@redis.railway.internal:6379"
```

### Cost Estimate
- **Starter Plan**: $5/month (500 hours)
- **Developer Plan**: $20/month (unlimited)
- Redis: ~$10-20/month

---

## Fly.io Deployment

### Setup

```bash
# Install Fly CLI
curl -L https://fly.io/install.sh | sh

# Login
fly auth login

# Launch app
fly launch --config fly.toml

# Create volume for database
fly volumes create aimate_data --size 1

# Deploy
fly deploy

# Add Redis
fly redis create

# Set secrets
fly secrets set AIMATE_DB_PASSWORD="your-secure-password"
fly secrets set REDIS_CONNECTION="redis://..."
```

### Cost Estimate (1M users)
- **Compute**: ~$2,000-3,000/month (autoscaling)
- **Redis**: ~$500-1,000/month
- **Storage**: ~$50/month

---

## Cloudflare Pages

### Frontend Deployment

```bash
# Install Wrangler
npm i -g wrangler

# Login
wrangler login

# Deploy
wrangler pages deploy dist --project-name=ai-mate
```

### Advantages
- **Free tier**: Unlimited bandwidth
- **300+ global edge locations**
- **DDoS protection** included
- **<50ms** response times globally

---

## Environment Variables

### Required

| Variable | Description | Example |
|----------|-------------|---------|
| `AIMATE_DB_PASSWORD` | Database encryption password | `secure-passphrase-2024` |
| `ASPNETCORE_ENVIRONMENT` | Environment | `Production` |
| `FRONTEND_ORIGINS` | Allowed CORS origins | `https://app.example.com` |

### Optional (Performance)

| Variable | Description | Default |
|----------|-------------|---------|
| `REDIS_CONNECTION` | Redis connection string | None (uses memory) |
| `ApplicationInsights__ConnectionString` | Azure monitoring | None |
| `INVOICE_SEED_MIN` | Min invoices for seeding | 250 |
| `INVOICE_SEED_MAX` | Max invoices for seeding | 450 |
| `JOB_SEED_MIN` | Min jobs for seeding | 80 |
| `JOB_SEED_MAX` | Max jobs for seeding | 200 |

---

## Monitoring & Observability

### Application Insights (Azure)

```bash
# Get connection string
az monitor app-insights component show \
  --app ai-mate-insights \
  --resource-group rg-ai-mate \
  --query connectionString -o tsv

# Set as environment variable
az containerapp update \
  --name ai-mate-api \
  --resource-group rg-ai-mate \
  --set-env-vars ApplicationInsights__ConnectionString="..."
```

### Log Queries (Serilog)

Logs are written to:
- Console (viewable in container logs)
- `logs/ai-mate-YYYY-MM-DD.log` files

### Health Checks

- **Simple**: `GET /health` - Quick OK check
- **Detailed**: `GET /api/health` - Comprehensive health report

Example response:
```json
{
  "ok": true,
  "status": "Healthy",
  "time": "2025-09-30T23:00:00Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "Database is responsive",
      "duration": 12.5
    },
    {
      "name": "redis",
      "status": "Healthy",
      "duration": 3.2
    },
    {
      "name": "memory",
      "status": "Healthy",
      "duration": 0.1
    }
  ]
}
```

---

## Cost Optimization

### Tier Recommendations

**0-10K users** (MVP/Beta)
- **Platform**: Railway or Fly.io
- **Cost**: ~$50-100/month
- **Why**: Simple, fast deployment, good free tiers

**10K-100K users** (Growth)
- **Platform**: Azure Container Apps + Cosmos DB
- **Cost**: ~$500-1,500/month
- **Why**: Auto-scaling, global distribution, enterprise features

**100K-1M users** (Scale)
- **Platform**: Multi-cloud (Cloudflare + Azure/AWS)
- **Cost**: ~$3,000-9,000/month
- **Why**: Best performance/cost ratio, vendor independence

**1M+ users** (Enterprise)
- **Platform**: Azure + Cloudflare + dedicated Redis
- **Cost**: ~$10,000-30,000/month
- **Why**: Full control, compliance, SLA guarantees

### Cost Reduction Tips

1. **Use Redis caching** - Reduces database queries by 80%+
2. **Enable gzip compression** - Reduces bandwidth costs
3. **Implement CDN** - Cloudflare is free and excellent
4. **Use reserved instances** - Save 30-70% on compute
5. **Monitor and alert** - Catch issues before they cost money
6. **Auto-scaling policies** - Scale down during off-peak hours

---

## Troubleshooting

### Common Issues

**1. Database locked errors**
```bash
# Check for multiple connections
# Solution: Use connection pooling (already configured)
```

**2. Redis connection failures**
```bash
# App will fall back to in-memory caching
# Check: REDIS_CONNECTION environment variable
```

**3. High memory usage**
```bash
# Check /api/health endpoint for memory stats
# Solution: Increase container memory or add more instances
```

**4. Rate limiting too aggressive**
```bash
# Adjust in Program.cs:
# Period = "1m", Limit = 100 (current)
# Increase for production load
```

---

## Next Steps

1. ✅ Choose deployment platform based on your user projections
2. ✅ Set up monitoring and alerts
3. ✅ Configure auto-scaling policies
4. ✅ Implement backup strategy
5. ✅ Set up CI/CD pipelines
6. ✅ Load test before launch
7. ✅ Document runbooks for common issues
8. ✅ Set up status page (e.g., status.example.com)

---

## Support & Resources

- **Azure Docs**: https://docs.microsoft.com/azure
- **Railway Docs**: https://docs.railway.app
- **Fly.io Docs**: https://fly.io/docs
- **Cloudflare Docs**: https://developers.cloudflare.com

For assistance, open an issue on GitHub or contact support@example.com
