# Tenant Workspace Page: Connectors

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Integrations |
| Page ID | PG:TW:04.1 |

## Purpose

Manage tenant connector instances (auth status, sync health).

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetTenantConnectors | GraphQL | connectorId, platform, status, lastSyncAt | Confidential | N | |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:04.1:01 | Connectors Table | connectors | [{connectorId,platform,status,lastSyncAt}] | Confidential | N | Health badges |
| UI:TW:04.1:02 | Add Connector Button | N/A | {} | Internal | N | Starts wizard |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:04.1:01 | Add Connector | EVT:connector.instance.added | WF:connector-add |
| ACT:TW:04.1:02 | Remove Connector | EVT:connector.instance.removed | WF:connector-remove |

---
Navigation: [Poll Analytics](tw_pg_02_3_poll_analytics.md) | Next: [Ask](tw_pg_05_1_ask.md)
