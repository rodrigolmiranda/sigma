# Global Admin Portal Sitemap

Portal ID: PORTAL_GLOBAL_ADMIN ("GA")

Scope: Internal / owner-level multi-tenant administration: tenants, plans, revenue, platform health, compliance, rollouts.

## Module Index

| Module ID | Name | Purpose | Key Pages | Core Aggregates | Primary Metrics |
|-----------|------|---------|-----------|-----------------|-----------------|
| MOD:GA:01 | Tenants & Onboarding | Manage tenant lifecycle | PG:GA:01.1 Tenants List; PG:GA:01.2 Tenant Detail | Tenant, User | tenant_count_active, churn_rate |
| MOD:GA:02 | Plans & Billing | Plan catalog & invoices | PG:GA:02.1 Plans; PG:GA:02.2 Invoices | Plan, Subscription, Invoice | mrr, arr, ltv, arpu |
| MOD:GA:03 | Revenue & Finance | Financial KPIs | PG:GA:03.1 Revenue Dashboard | Subscription, Invoice | mrr, arr, churn_rate, ltv |
| MOD:GA:04 | Usage & Entitlements | Usage-based allocation | PG:GA:04.1 Usage Overview | UsageRecord, AddOnEntitlement | message_volume, poll_usage |
| MOD:GA:05 | Platform Health | Operational metrics | PG:GA:05.1 Health Dashboard | IngestStat, QueueStat | ingest_latency_p95, queue_depth |
| MOD:GA:06 | Compliance & Security | Audit & policy status | PG:GA:06.1 Compliance Overview | AuditEvent, RetentionPolicy | audit_events_last_24h |
| MOD:GA:07 | Feature Flags & Rollouts | Control experimental features | PG:GA:07.1 Flags | FeatureFlag | flags_active |
| MOD:GA:08 | Integrations Registry | Global connector status | PG:GA:08.1 Connectors | ConnectorDefinition | connectors_active |
| MOD:GA:09 | Internal Users & Roles | Internal staff RBAC | PG:GA:09.1 Users | InternalUser | internal_user_active |
| MOD:GA:10 | Support & Incidents | Incident oversight | PG:GA:10.1 Incidents | Incident | open_incidents |
| MOD:GA:11 | Settings | Global configuration | PG:GA:11.1 Global Settings | GlobalConfig | config_version |

 
## Page Detail: Tenants List

Page ID: PG:GA:01.1
Purpose: List tenants with key health & financial indicators.

### Data Sources (Tenants List)

| Query/Source | Type | ID | Fields (Key) | Notes |
|--------------|------|----|--------------|-------|
| GQL:GetTenantList | GraphQL Query | GQL:GetTenantList | tenantId, name, status, planId, churnRiskScore | Filter & pagination |
| SQLV:vw_tenant_financials | SQL View | SQLV:vw_tenant_financials | tenant_id, mrr, arr, ltv, arpu | Pre-aggregated monthly refresh |
| SQLV:vw_tenant_usage_daily | SQL View | SQLV:vw_tenant_usage_daily | tenant_id, messages_30d, polls_30d | Rolling 30d activity |

### UI Elements (Tenants List)

| UI ID | Type | Data Source | Shape (Excerpt) | Notes |
|-------|------|-------------|-----------------|-------|
| UI:GA:01.1:01 | Table "Tenants" | GQL:GetTenantList + joined financial views | [{tenantId, name, status, planName, mrr, messages30d}] | Virtual scroll |
| UI:GA:01.1:02 | Filter Bar | N/A | {status[], planId, searchTerm} | Debounced search |
| UI:GA:01.1:03 | Column: Risk Badge | churnRiskScore | {score, band} | Color-coded |
| UI:GA:01.1:04 | Action Menu | Row context | {actions[]} | Conditional by status |
| UI:GA:01.1:05 | Pagination | GQL metadata | {page, size, total} | Standard |

### Actions & Events (Tenants List)

| Action ID | Action | Trigger | Event Emitted | Workflow |
|-----------|--------|---------|---------------|----------|
| ACT:GA:01.1:01 | Create Tenant | Button | EVT:tenant.created | WF:tenant-onboard |
| ACT:GA:01.1:02 | Open Tenant Detail | Row click | (none) | WF:tenant-drill |
| ACT:GA:01.1:03 | Suspend Tenant | Menu item | EVT:tenant.suspended | WF:tenant-status-change |
| ACT:GA:01.1:04 | Resume Tenant | Menu item | EVT:tenant.resumed | WF:tenant-status-change |

### Domain Aggregates Referenced

| Aggregate | Read/Write | Fields (Key) | Invariants |
|-----------|-----------|--------------|------------|
| Tenant | R/W (create,suspend,resume) | tenantId, name, status, planId | status ∈ {active,suspended} |
| Subscription | R | planId, startDate, renewalDate | Renewal > startDate |

### Metrics Mapped

| Metric ID | Description | Source |
|-----------|-------------|--------|
| MET:ten:tenant_count_active | Active tenant count | Count(Tenant where status=active) |
| MET:fin:mrr | Monthly recurring revenue | Sum(mrr) |
| MET:fin:churn_risk_avg | Avg churn risk | Avg(churnRiskScore) |

---
 
## Page Detail: Tenant Detail

Page ID: PG:GA:01.2
Purpose: Single tenant 360° view (financial, usage, risk, entitlements).

### Data Sources (Tenant Detail)

| Source | Type | Fields | Notes |
|--------|------|--------|-------|
| GQL:GetTenant | GraphQL | tenantId, name, plan, status | Core identity |
| SQLV:vw_tenant_financials | View | mrr, arr, ltv, arpu, churn_rate | Financial snapshot |
| SQLV:vw_tenant_usage_daily | View | messages_7d, polls_7d, flags_7d | Usage trend |
| GQL:GetEntitlements | GraphQL | addons{guardian,askdb,broadcasts} limits | Feature toggles |

### UI Elements (Tenant Detail)

| UI ID | Type | Data | Shape | Notes |
|-------|------|------|-------|-------|
| UI:GA:01.2:01 | Header Summary | Aggregated | {tenantId,name,status,planName,mrr} | Status badge |
| UI:GA:01.2:02 | Usage Mini Charts | usage view | [{metric,value,trend}] | Sparkline 7d |
| UI:GA:01.2:03 | Add-ons Panel | entitlements | {addon,enabled,limit,usage} | Toggle gating |
| UI:GA:01.2:04 | Risk Panel | churnRiskScore | {score, band, drivers[]} | Drivers list |
| UI:GA:01.2:05 | Timeline | events feed | {eventType, ts, actor} | Paged |

### Actions & Events (Tenant Detail)

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:01.2:01 | Change Plan | EVT:tenant.plan.changed | WF:plan-change |
| ACT:GA:01.2:02 | Enable Add-on | EVT:tenant.addon.enabled | WF:addon-toggle |
| ACT:GA:01.2:03 | Disable Add-on | EVT:tenant.addon.disabled | WF:addon-toggle |
| ACT:GA:01.2:04 | Adjust Limit | EVT:tenant.limit.adjusted | WF:limit-adjust |

### Sample Aggregate JSON (Tenant)

```json
{
  "tenantId": "t_12345",
  "name": "Acme Cohort",
  "status": "active",
  "planId": "plan_core",
  "addons": {"guardian": true, "askdb": false, "broadcasts": true},
  "limits": {"messageMonthly": 1000000, "pollActive": 50},
  "financial": {"mrr": 499.00, "arr": 5988.00, "ltv": 7200.00, "churnRiskScore": 0.18}
}
```

---
 
## Page Detail: Revenue Dashboard

Page ID: PG:GA:03.1
Purpose: Financial KPI monitoring.

### Metrics (Revenue Dashboard)

| Metric ID | Definition | Formula |
|-----------|------------|---------|
| MET:fin:mrr | Current Monthly Recurring Revenue | Sum(subscriptions.mrr_current) |
| MET:fin:arr | Annualized Recurring Revenue | MET:fin:mrr * 12 |
| MET:fin:churn_rate | Logo churn (monthly) | lost_tenants / starting_tenants |
| MET:fin:ltv | Lifetime Value | arpu / churn_rate |

### UI Elements (Revenue Dashboard)

| UI ID | Type | Data | Notes |
|-------|------|------|-------|
| UI:GA:03.1:01 | KPI Tiles | metrics service | mrr, arr, churn, ltv |
| UI:GA:03.1:02 | MRR Trend Chart | timeseries | last 12 months |
| UI:GA:03.1:03 | Churn Breakdown | table | lost tenants by reason |
| UI:GA:03.1:04 | Plan Mix Pie | chart | revenue share by plan |

### Actions

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:GA:03.1:01 | Export CSV | EVT:finance.export.created | WF:finance-export |

---
 
## Additional Pages (Abbreviated Index)

Only headers here; each can be expanded later following same template.

| Page ID | Name | Summary |
|---------|------|---------|
| PG:GA:02.1 | Plans Catalog | View & manage plan definitions |
| PG:GA:02.2 | Invoices | Invoice listing & drill |
| PG:GA:04.1 | Usage Overview | Cross-tenant usage caps & anomalies |
| PG:GA:05.1 | Health Dashboard | Latency, queue depth, error rates |
| PG:GA:06.1 | Compliance Overview | Audit volume, retention status |
| PG:GA:07.1 | Feature Flags | Toggle flags & target cohorts |
| PG:GA:08.1 | Connectors Registry | Connector availability & versions |
| PG:GA:09.1 | Internal Users | Internal staff roles |
| PG:GA:10.1 | Incidents | Open/closed incidents & SLAs |
| PG:GA:11.1 | Global Settings | Platform-wide parameters |

---
Navigation: [Home](home.md) | Previous: [ID Reference Matrix](id_reference_matrix.md) | Next: [Tenant Workspace Portal Sitemap](portal_tenant_workspace_sitemap.md)
