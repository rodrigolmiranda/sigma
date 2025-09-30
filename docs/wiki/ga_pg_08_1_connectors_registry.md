# Global Admin Page: Connectors Registry

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Integrations Team |
| Page ID | PG:GA:08.1 |

## Purpose

View all connector definitions, versions, deprecation status.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetConnectorDefinitions | GraphQL | connectorId, name, version, status | Internal | N | Version history |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:08.1:01 | Connectors Table | definitions | [{connectorId,name,version,status}] | Internal | N | Badge by status |
| UI:GA:08.1:02 | Deprecation Toggle | row | {status} | Internal | N | Confirm dialog |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:08.1:01 | Deprecate Connector | EVT:connector.deprecated | WF:connector-deprecate |
| ACT:GA:08.1:02 | Undeprecate Connector | EVT:connector.undeprecated | WF:connector-deprecate |

---
Navigation: [Feature Flags](ga_pg_07_1_feature_flags.md) | Next: [Internal Users](ga_pg_09_1_internal_users.md)
