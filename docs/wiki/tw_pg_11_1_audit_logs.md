# Tenant Workspace Page: Audit Logs

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Security/Compliance |
| Page ID | PG:TW:11.1 |

## Purpose

Browse and export tenant-scoped audit events.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetAuditEvents | GraphQL | eventId, type, actorHash, ts | Confidential | Y | actor masked |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:11.1:01 | Audit Table | events | [{eventId,type,actorHash,ts}] | Confidential | Y | Filter range |
| UI:TW:11.1:02 | Export Button | selection | {filters} | Internal | N | Async job |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:11.1:01 | Export Audit | EVT:audit.export.created | WF:audit-export |

---
Navigation: [Settings](tw_pg_10_1_settings.md) | Next: [Data Exports](tw_pg_12_1_data_exports.md)
