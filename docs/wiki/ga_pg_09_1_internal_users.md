# Global Admin Page: Internal Users

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Internal Ops |
| Page ID | PG:GA:09.1 |

## Purpose

Manage internal staff accounts and RBAC roles.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetInternalUsers | GraphQL | userId, emailHash, roles[] | Confidential | Y | Hash only |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:GA:09.1:01 | Users Table | users | [{userId,emailHash,roles}] | Confidential | Y | Role badges |
| UI:GA:09.1:02 | Invite Button | N/A | {} | Internal | N | Sends email |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:09.1:01 | Invite User | EVT:internal.user.invited | WF:internal-user-invite |
| ACT:GA:09.1:02 | Change Roles | EVT:internal.user.roles.changed | WF:internal-user-role-change |

---
Navigation: [Connectors Registry](ga_pg_08_1_connectors_registry.md) | Next: [Incidents](ga_pg_10_1_incidents.md)
