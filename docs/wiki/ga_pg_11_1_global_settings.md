# Global Admin Page: Global Settings

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Platform Ops |
| Page ID | PG:GA:11.1 |

## Purpose

Modify platform-wide configuration (regional defaults, limits, legal texts).

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetGlobalConfig | GraphQL | configVersion, defaults, limits | Internal | N | Versioned |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:11.1:01 | Config Editor | config | {section,values} | Internal | N | JSON diff |
| UI:GA:11.1:02 | Save Button | form state | {disabled} | Internal | N | Validates schema |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:11.1:01 | Save Config | EVT:global.config.saved | WF:global-config-save |

---
Navigation: [Incidents](ga_pg_10_1_incidents.md) | Back: [Global Admin Portal Sitemap](portal_global_admin_sitemap.md)
