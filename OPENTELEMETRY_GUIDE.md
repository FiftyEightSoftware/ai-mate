# OpenTelemetry Instrumentation Guide

**Status:** ✅ **Fully Implemented**  
**Date:** October 2, 2025  
**Version:** 1.0.0

---

## 📊 Overview

AI Mate is now fully instrumented with OpenTelemetry for complete observability. The system captures:
- **Metrics** - Business KPIs, performance metrics, system health
- **Traces** - Distributed tracing across all operations
- **Logs** - Structured logging with Serilog (existing)

---

## 🎯 What's Instrumented

### Automatic Instrumentation
- ✅ **HTTP Requests** - All incoming/outgoing HTTP calls
- ✅ **Database Queries** - SQL operations with query text
- ✅ **Redis Operations** - Cache hits/misses
- ✅ **Runtime Metrics** - GC, memory, thread pool
- ✅ **Process Metrics** - CPU, memory usage

### Business Metrics
| Metric | Type | Description |
|--------|------|-------------|
| `ai_mate.invoice.created` | Counter | Number of invoices created |
| `ai_mate.invoice.paid` | Counter | Number of invoices marked paid |
| `ai_mate.invoice.amount` | Histogram | Distribution of invoice amounts (GBP) |
| `ai_mate.job.created` | Counter | Number of jobs created |
| `ai_mate.job.quote_amount` | Histogram | Distribution of job quote amounts (GBP) |
| `ai_mate.dashboard.viewed` | Counter | Dashboard page views |
| `ai_mate.database.query` | Counter | Database query count |
| `ai_mate.cache.hit` | Counter | Cache hit count |
| `ai_mate.cache.miss` | Counter | Cache miss count |
| `ai_mate.api.response_time` | Histogram | API response times (ms) |
| `ai_mate.api.error` | Counter | API error count by status code |

### Custom Traces
| Operation | Attributes |
|-----------|------------|
| `GetDashboard` | `invoice.count`, `outstanding.total`, `overdue.total` |
| `MarkInvoicePaid` | `invoice.id`, `result` |
| `CreateJob` | `job.title`, `job.status`, `job.quoted_price` |

---

## 🚀 Quick Start

### Development (Console Exporter)

Metrics and traces are automatically logged to console in development:

```bash
cd backend
dotnet run
```

You'll see OpenTelemetry output like:
```
Export ai-mate-api, Meter: ai-mate-api/1.0.0
(2025-10-02T16:00:00.000Z, 2025-10-02T16:00:30.000Z] ai_mate.dashboard.viewed Metric data: Sum 5
(2025-10-02T16:00:00.000Z, 2025-10-02T16:00:30.000Z] ai_mate.invoice.paid Metric data: Sum 2
```

### Production (OTLP Exporter)

Export to any OpenTelemetry collector (Jaeger, Prometheus, Grafana, etc.):

```bash
# Set environment variable
export OTEL_EXPORTER_OTLP_ENDPOINT="http://your-collector:4317"

# Or in render.yaml / docker-compose
OTEL_EXPORTER_OTLP_ENDPOINT=http://jaeger:4317
```

---

## 📈 Integration Options

### Option 1: Jaeger (Distributed Tracing)

**Run Jaeger locally:**
```bash
docker run -d --name jaeger \
  -p 16686:16686 \
  -p 4317:4317 \
  jaegertracing/all-in-one:latest

# Set endpoint
export OTEL_EXPORTER_OTLP_ENDPOINT="http://localhost:4317"

# View UI
open http://localhost:16686
```

**What you'll see:**
- Complete request traces from frontend → backend → database
- Performance bottlenecks highlighted
- Error traces with full context

### Option 2: Prometheus + Grafana (Metrics)

**docker-compose.yml:**
```yaml
version: '3.8'
services:
  prometheus:
    image: prom/prometheus
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml

  grafana:
    image: grafana/grafana
    ports:
      - "3000:3000"
    environment:
      - GF_SECURITY_ADMIN_PASSWORD=admin
```

**prometheus.yml:**
```yaml
scrape_configs:
  - job_name: 'ai-mate'
    scrape_interval: 15s
    static_configs:
      - targets: ['localhost:5280']
```

### Option 3: Application Insights (Azure)

Already integrated! Just set:
```bash
export ApplicationInsights__ConnectionString="InstrumentationKey=..."
```

OpenTelemetry data automatically flows to Azure Monitor.

### Option 4: Grafana Cloud (Managed)

```bash
# Get OTLP endpoint from Grafana Cloud
export OTEL_EXPORTER_OTLP_ENDPOINT="https://otlp-gateway-prod-us-central-0.grafana.net/otlp"
export OTEL_EXPORTER_OTLP_HEADERS="Authorization=Basic <base64-token>"
```

---

## 🔍 Viewing Metrics

### Console Output (Development)

Metrics are exported every 30 seconds to console:

```log
Export ai-mate-api, Meter: ai-mate-api/1.0.0
Metric: ai_mate.dashboard.viewed
  Type: Sum
  Temporality: Cumulative
  Value: 42

Metric: ai_mate.api.response_time
  Type: Histogram
  Temporality: Cumulative
  Count: 100
  Sum: 5234.5ms
  Min: 12.3ms
  Max: 234.1ms
  Buckets: [0-50ms: 45, 50-100ms: 30, 100-200ms: 20, 200+ms: 5]
```

### Querying Metrics (Prometheus)

```promql
# Average API response time
rate(ai_mate_api_response_time_sum[5m]) / rate(ai_mate_api_response_time_count[5m])

# Invoice creation rate
rate(ai_mate_invoice_created[5m])

# Error rate
rate(ai_mate_api_error[5m])

# Cache hit ratio
rate(ai_mate_cache_hit[5m]) / (rate(ai_mate_cache_hit[5m]) + rate(ai_mate_cache_miss[5m]))
```

---

## 🎨 Dashboard Examples

### Grafana Dashboard JSON

```json
{
  "dashboard": {
    "title": "AI Mate - Business Metrics",
    "panels": [
      {
        "title": "Dashboard Views",
        "targets": [{
          "expr": "rate(ai_mate_dashboard_viewed[5m])"
        }]
      },
      {
        "title": "Invoice Operations",
        "targets": [
          {"expr": "rate(ai_mate_invoice_created[5m])", "legendFormat": "Created"},
          {"expr": "rate(ai_mate_invoice_paid[5m])", "legendFormat": "Paid"}
        ]
      },
      {
        "title": "API Response Time (p95)",
        "targets": [{
          "expr": "histogram_quantile(0.95, rate(ai_mate_api_response_time_bucket[5m]))"
        }]
      }
    ]
  }
}
```

---

## 🔧 Configuration

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `OTEL_USE_CONSOLE` | `true` | Enable console exporter (dev) |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | `""` | OTLP collector endpoint |
| `OTEL_SERVICE_NAME` | `ai-mate-api` | Service name in telemetry |
| `OTEL_RESOURCE_ATTRIBUTES` | Auto | Additional resource attributes |

### Render Deployment

Add to Render environment variables:
```bash
OTEL_EXPORTER_OTLP_ENDPOINT=https://your-collector.com:4317
OTEL_USE_CONSOLE=false  # Disable console in production
```

---

## 📊 Key Dashboards to Create

### 1. Business Health Dashboard
- Invoice creation rate (per hour/day)
- Payment processing rate
- Outstanding vs paid ratio
- Average invoice amount
- Job creation rate
- Quote amount distribution

### 2. API Performance Dashboard
- Request rate (RPM)
- Response time percentiles (p50, p95, p99)
- Error rate by endpoint
- Slow queries (>1s)
- Cache hit ratio

### 3. System Health Dashboard
- CPU usage
- Memory usage
- GC frequency
- Thread pool saturation
- Database connection pool
- Redis connection health

---

## 🚨 Alerting Examples

### Prometheus Alertmanager

```yaml
groups:
  - name: ai_mate_alerts
    rules:
      - alert: HighErrorRate
        expr: rate(ai_mate_api_error[5m]) > 0.05
        annotations:
          summary: "High API error rate detected"
          
      - alert: SlowAPIResponses
        expr: histogram_quantile(0.95, rate(ai_mate_api_response_time_bucket[5m])) > 1000
        annotations:
          summary: "95th percentile response time > 1s"
          
      - alert: LowCacheHitRate
        expr: rate(ai_mate_cache_hit[5m]) / (rate(ai_mate_cache_hit[5m]) + rate(ai_mate_cache_miss[5m])) < 0.7
        annotations:
          summary: "Cache hit rate below 70%"
```

---

## 📝 Custom Instrumentation

### Adding New Metrics

```csharp
// In OpenTelemetryConfig.cs
public static readonly Counter<long> CustomMetric = AppMeter.CreateCounter<long>(
    "ai_mate.custom.metric",
    description: "Description of metric");

// In your code
OpenTelemetryConfig.CustomMetric.Add(1, 
    new KeyValuePair<string, object?>("tag_name", "value"));
```

### Adding New Traces

```csharp
using var activity = OpenTelemetryConfig.StartActivity("OperationName");
activity?.SetTag("custom.attribute", value);

// Your operation here

activity?.SetStatus(ActivityStatusCode.Ok);
```

### Recording Events

```csharp
OpenTelemetryConfig.RecordEvent("UserAction", new Dictionary<string, object>
{
    ["user.id"] = userId,
    ["action.type"] = "invoice_paid",
    ["invoice.amount"] = amount
});
```

---

## 🧪 Testing Instrumentation

### Generate Test Traffic

```bash
# Dashboard views
for i in {1..10}; do curl http://localhost:5280/api/dashboard; done

# Create invoices (if endpoint exists)
curl -X POST http://localhost:5280/api/jobs \
  -H "Content-Type: application/json" \
  -d '{"title":"Test Job","status":"Upcoming","quotedPrice":500}'

# Mark invoice paid
curl -X POST http://localhost:5280/api/invoices/INVOICE_ID/mark-paid \
  -H "Content-Type: application/json" \
  -d '{"paidDate":"2025-10-02"}'
```

### View Metrics

```bash
# Watch console output
dotnet run | grep "ai_mate"

# Or query Prometheus
curl http://localhost:9090/api/v1/query?query=ai_mate_dashboard_viewed
```

---

## 📚 Resources

### OpenTelemetry Documentation
- **Main Site:** https://opentelemetry.io
- **.NET SDK:** https://github.com/open-telemetry/opentelemetry-dotnet
- **Semantic Conventions:** https://opentelemetry.io/docs/specs/semconv/

### Integration Guides
- **Jaeger:** https://www.jaegertracing.io/docs/getting-started/
- **Prometheus:** https://prometheus.io/docs/prometheus/latest/getting_started/
- **Grafana:** https://grafana.com/docs/grafana/latest/getting-started/
- **Azure Monitor:** https://docs.microsoft.com/en-us/azure/azure-monitor/app/opentelemetry-enable

### Best Practices
- Use consistent naming conventions (ai_mate.*)
- Add meaningful tags/attributes
- Don't log sensitive data (passwords, PII)
- Sample high-volume traces (if needed)
- Set appropriate cardinality limits

---

## 🎯 Next Steps

1. ✅ **Deploy with instrumentation** - Push to production
2. ⏭️ **Set up Grafana** - Create dashboards
3. ⏭️ **Configure alerts** - Get notified of issues
4. ⏭️ **Analyze traces** - Find performance bottlenecks
5. ⏭️ **Monitor business KPIs** - Track invoice/job metrics

---

## 💡 Tips

### Performance Impact
OpenTelemetry adds minimal overhead (<1% CPU, <50MB RAM). The benefits far outweigh the cost.

### Sampling
For high-traffic scenarios, consider sampling:
```csharp
.SetSampler(new TraceIdRatioBasedSampler(0.1)) // Sample 10% of traces
```

### Security
Never log sensitive data. Use `[SuppressInstrumentation]` for auth endpoints if needed.

---

**Your application is now fully observable! 🎉**

Monitor, measure, and improve with confidence.
