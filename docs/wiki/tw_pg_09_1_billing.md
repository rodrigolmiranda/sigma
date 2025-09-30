# Tenant Workspace Page: Billing

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Finance Ops |
| Page ID | PG:TW:09.1 |

## Purpose

View current plan, usage overages, invoices.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetTenantSubscription | GraphQL | planId, renewalDate, mrr | Confidential | N | |
| GQL:GetTenantInvoices | GraphQL | invoiceId, total, status | Confidential | N | Paginated |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:09.1:01 | Plan Panel | subscription | {planId,mrr,renewalDate} | Confidential | N | |
| UI:TW:09.1:02 | Invoices Table | invoices | [{invoiceId,total,status}] | Confidential | N | |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:09.1:01 | Download Invoice | EVT:invoice.download.requested | WF:invoice-download |

---
Navigation: [Users](tw_pg_08_1_users.md) | Next: [Settings](tw_pg_10_1_settings.md)
