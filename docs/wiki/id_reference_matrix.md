# ID Reference Matrix

Purpose: Single namespace of stable IDs for cross-document traceability (site maps, events, workflows, actions, UI elements, domain aggregates, metrics, tests).

## ID Conventions

| Kind | Pattern | Example |
|------|---------|---------|
| Portal | PORTAL_&lt;SCOPE&gt; | PORTAL_GLOBAL_ADMIN |
| Module | MOD:&lt;PORTAL&gt;:&lt;SEQ&gt; | MOD:GA:01 |
| Page | PG:&lt;PORTAL&gt;:&lt;SEQ&gt;[.&lt;SUB&gt;] | PG:GA:01.1 |
| UI Element | UI:&lt;PAGE&gt;:&lt;SEQ&gt; | UI:GA:01.1:05 |
| Action | ACT:&lt;PAGE&gt;:&lt;SEQ&gt; | ACT:GA:01.1:02 |
| Workflow | WF:&lt;NAME&gt; (kebab) | WF:tenant-onboard |
| Event | EVT:&lt;dotted-domain&gt; | EVT:poll.vote.recorded |
| Aggregate | AGG:&lt;Name&gt; | AGG:Poll |
| Metric | MET:&lt;category&gt;:&lt;name&gt; | MET:rev:arr |
| GraphQL Query | GQL:&lt;operationName&gt; | GQL:GetTenantList |
| SQL View | SQLV:&lt;view_name&gt; | SQLV:vw_tenant_revenue_monthly |

All IDs are immutable once published. Deprecated IDs marked in a future Deprecated section.

## Cross Mapping (Excerpt)

| ID | Domain | Source Doc | Notes |
|----|--------|-----------|-------|
| PG:GA:01.1 | Page | portal_global_admin_sitemap.md | Tenants List |
| ACT:GA:01.1:02 | Action | portal_global_admin_sitemap.md | Open Tenant Detail |
| EVT:poll.vote.recorded | Event | event_catalog.md | Poll vote captured |
| MET:rev:arr | Metric | portal_global_admin_sitemap.md | Annual recurring revenue |
| UI:TW:02.2:03 | UI | portal_tenant_workspace_sitemap.md | Poll Create Submit Button |

---
Navigation: [Home](home.md) | Previous: [Governance & Change Control](governance_change_control.md) | Next: [Global Admin Portal Sitemap](portal_global_admin_sitemap.md)
