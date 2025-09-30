# Tenant Workspace Page: Custom Reports

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Product Analytics |
| Page ID | PG:TW:07.3 |

## Purpose

User-defined query builder producing exportable result sets.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| API:reportQuery | Service | queryDSL, result[] | Confidential | N | Query validated |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:07.3:01 | Query Builder | form | {filters,metrics,groupBy} | Confidential | N | DSL assist |
| UI:TW:07.3:02 | Results Table | execution | rows[] | Confidential | N | Virtual scroll |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:07.3:01 | Run Report | EVT:report.executed | WF:report-execute |
| ACT:TW:07.3:02 | Export Results | EVT:report.export.created | WF:report-export |

---
Navigation: [Safety Analytics](tw_pg_07_2_safety_analytics.md) | Next: [Users](tw_pg_08_1_users.md)
