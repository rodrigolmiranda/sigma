# Tenant Workspace Page: Engagement Analytics

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Product Analytics |
| Page ID | PG:TW:07.1 |

## Purpose

Deep engagement metrics (messages volume, poll participation, broadcast reach trends).

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| SQLV:vw_engagement_timeseries | View | day, metric, value | Confidential | N | Normalized |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:07.1:01 | Metric Selector | local | {metrics[]} | Internal | N | |
| UI:TW:07.1:02 | Timeseries Chart | view | [{day,value}] | Confidential | N | Multi-line |

---
Navigation: [Broadcast History](tw_pg_06_2_broadcast_history.md) | Next: [Safety Analytics](tw_pg_07_2_safety_analytics.md)
