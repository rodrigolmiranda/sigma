# Tenant Workspace Page: Settings

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Tenant Admin |
| Page ID | PG:TW:10.1 |

## Purpose

Manage retention, privacy, locale preferences.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetTenantSettings | GraphQL | retentionDays, locale, timeZone | Confidential | N | |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:10.1:01 | Settings Form | settings | {retentionDays,locale,timeZone} | Confidential | N | |
| UI:TW:10.1:02 | Save Button | form | {disabled} | Internal | N | Validation |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:10.1:01 | Save Settings | EVT:tenant.settings.saved | WF:tenant-settings-save |

---
Navigation: [Billing](tw_pg_09_1_billing.md) | Next: [Audit Logs](tw_pg_11_1_audit_logs.md)
