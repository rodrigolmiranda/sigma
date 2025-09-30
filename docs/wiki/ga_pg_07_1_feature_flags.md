# Global Admin Page: Feature Flags

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Engineering |
| Page ID | PG:GA:07.1 |

## Purpose

Enable/disable features or target partial cohorts; manage rollout strategies.

## Data Sources

| Source | Type | Fields | Notes | Class | PII |
|--------|------|--------|-------|-------|-----|
| GQL:GetFeatureFlags | GraphQL | flagId, name, status, rollout{percent} | Listing | Internal | N |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:07.1:01 | Flags Table | flags | [{flagId,name,status,percent}] | Internal | N | Inline edit percent |
| UI:GA:07.1:02 | Create Flag Button | N/A | {} | Internal | N | Modal |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:07.1:01 | Create Flag | EVT:flag.created | WF:flag-create |
| ACT:GA:07.1:02 | Update Rollout | EVT:flag.rollout.updated | WF:flag-update |
| ACT:GA:07.1:03 | Archive Flag | EVT:flag.archived | WF:flag-archive |

---
Navigation: [Compliance Overview](ga_pg_06_1_compliance_overview.md) | Next: [Connectors Registry](ga_pg_08_1_connectors_registry.md)
