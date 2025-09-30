# Tenant Workspace Page: Safety Analytics

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Safety Team |
| Page ID | PG:TW:07.2 |

## Purpose

Analyze violation trends and rule performance.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| SQLV:vw_violation_timeseries | View | day, severity, count | Confidential | N | Aggregated |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:07.2:01 | Severity Stack Chart | timeseries | stacked bars | Confidential | N | 30d |
| UI:TW:07.2:02 | Rule Performance Table | derived | [{ruleId,count,avgTimeToResolve}] | Confidential | N | Sortable |

---
Navigation: [Engagement Analytics](tw_pg_07_1_engagement_analytics.md) | Next: [Custom Reports](tw_pg_07_3_custom_reports.md)
