# Global Admin Page: Compliance Overview

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Security/Compliance |
| Page ID | PG:GA:06.1 |

## Purpose

Track audit volume, data retention adherence, upcoming certification tasks.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| SQLV:vw_audit_counts | View | day, count | Confidential | N | Aggregated |
| GQL:GetRetentionViolations | GraphQL | tenantId, objectType, ageDays | Confidential | N | Violations only |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:06.1:01 | Audit Volume Chart | audit counts | timeSeries | Confidential | N | 30d |
| UI:GA:06.1:02 | Retention Violations Table | violations | [{tenantId,objectType,ageDays}] | Confidential | N | Action links |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:06.1:01 | Export Compliance Report | EVT:compliance.report.exported | WF:compliance-export |

---
Navigation: [Health Dashboard](ga_pg_05_1_health_dashboard.md) | Next: [Feature Flags](ga_pg_07_1_feature_flags.md)
