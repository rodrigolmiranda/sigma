# Tenant Workspace Page: Users

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Tenant Admin |
| Page ID | PG:TW:08.1 |

## Purpose

Manage tenant user accounts and role assignments.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetTenantUsers | GraphQL | userId, emailHash, roles[] | Confidential | Y | Hash only |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:08.1:01 | Users Table | users | [{userId,emailHash,roles}] | Confidential | Y | Role badges |
| UI:TW:08.1:02 | Invite Button | N/A | {} | Internal | N | |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:08.1:01 | Invite User | EVT:user.invited | WF:user-invite |
| ACT:TW:08.1:02 | Change Roles | EVT:user.roles.changed | WF:user-role-change |

---
Navigation: [Custom Reports](tw_pg_07_3_custom_reports.md) | Next: [Billing](tw_pg_09_1_billing.md)
