# Global Admin Page: Usage Overview

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Platform Ops |
| Page ID | PG:GA:04.1 |

## Purpose

Cross-tenant usage summarization (messages, polls, add-on consumption) for capacity planning.

## Data Sources

| Source | Type | Fields | Notes | Class | PII |
|--------|------|--------|-------|-------|-----|
| SQLV:vw_usage_daily | View | tenant_id, metric, value_30d | Rolling window | Confidential | N |
| GQL:GetUsageAnomalies | GraphQL | tenantId, metric, score | ML anomaly scores | Internal | N |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:04.1:01 | Heatmap | usage view | matrix(tenant vs metric) | Confidential | N | Lazy chunk load |
| UI:GA:04.1:02 | Anomaly Table | anomalies | [{tenantId,metric,score}] | Internal | N | Score sorting |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:04.1:01 | Export Usage Snapshot | EVT:usage.snapshot.exported | WF:usage-export |

## Metrics

| Metric ID | Definition |
|-----------|------------|
| MET:ops:ingest_latency_p95 | p95 ingest latency (related) |

---
Navigation: [Invoices](ga_pg_02_2_invoices.md) | Next: [Health Dashboard](ga_pg_05_1_health_dashboard.md)
