# Tenant Workspace Page: Poll Analytics

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Product |
| Page ID | PG:TW:02.3 |

## Purpose

Detailed analytics for a selected poll (option distribution, engagement over time, platform breakdown).

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| SQLV:vw_poll_timeseries | View | poll_id, ts, voters_cum | Confidential | N | Timeseries |
| SQLV:vw_poll_platform_breakdown | View | poll_id, platform, voters | Confidential | N | Platform split |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:02.3:01 | Engagement Chart | timeseries | [{ts,voters}] | Confidential | N | Line chart |
| UI:TW:02.3:02 | Platform Breakdown | breakdown | [{platform,voters}] | Confidential | N | Pie |
| UI:TW:02.3:03 | Option Table | poll | [{option,text,votes}] | Confidential | N | Sorted desc |

---
Navigation: [Dashboard](tw_pg_01_1_dashboard.md) | Next: [Connectors](tw_pg_04_1_connectors.md)
