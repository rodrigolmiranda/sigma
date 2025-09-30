# Tenant Workspace Portal Sitemap

Portal ID: PORTAL_TENANT_WORKSPACE ("TW")

Scope: Tenant-facing operational & analytical interface.

## Module Index

| Module ID | Name | Purpose | Key Pages | Aggregates | Primary Metrics |
|-----------|------|---------|-----------|------------|-----------------|
| MOD:TW:01 | Dashboard | Engagement & health snapshot | PG:TW:01.1 Dashboard | Message, Poll, GuardianViolation | engagement_rate, open_flags |
| MOD:TW:02 | Polls | Create & manage polls | PG:TW:02.1 Polls List; PG:TW:02.2 Poll Create; PG:TW:02.3 Poll Analytics | Poll, PollVote | poll_engagement_rate |
| MOD:TW:03 | Guardian | Safety triage & rules | PG:TW:03.1 Violations Queue; PG:TW:03.2 Rules | GuardianViolation, Rule | triage_time_mean |
| MOD:TW:04 | Connectors | Manage platform connectors | PG:TW:04.1 Connectors | ConnectorInstance | connectors_active |
| MOD:TW:05 | Ask‑DB | Knowledge Q&A & sources | PG:TW:05.1 Ask; PG:TW:05.2 Sources | KnowledgeSource, Embedding | rag_latency_p95 |
| MOD:TW:06 | Broadcasts | Outbound multi-platform messages | PG:TW:06.1 Composer; PG:TW:06.2 History | Broadcast, DeliveryResult | broadcast_reach |
| MOD:TW:07 | Analytics | Deep metrics & exports | PG:TW:07.1 Engagement; PG:TW:07.2 Safety; PG:TW:07.3 Custom Reports | MetricsDaily | messages_count |
| MOD:TW:08 | Users & Roles | Manage tenant users | PG:TW:08.1 Users | User | user_count |
| MOD:TW:09 | Billing & Plan | Plan, usage, invoices | PG:TW:09.1 Billing | Subscription, Invoice | mrr_tenant |
| MOD:TW:10 | Settings | Retention, privacy, limits | PG:TW:10.1 Settings | RetentionPolicy | retention_days |
| MOD:TW:11 | Audit Logs | View audit events | PG:TW:11.1 Audit Logs | AuditEvent | audit_events_7d |
| MOD:TW:12 | Data Export | Export raw data | PG:TW:12.1 Exports | ExportJob | export_jobs_active |

 
## Page Detail: Polls List

Page ID: PG:TW:02.1
Purpose: List tenant polls with status, engagement stats.

### Data Sources

| Source | Type | Fields | Notes |
|--------|------|--------|-------|
| GQL:GetPolls | GraphQL | pollId, question, status, createdAt | Filter by status |
| SQLV:vw_poll_engagement | SQL View | poll_id, voters_unique, engagement_rate | Daily refresh + on vote event |

### UI Elements (Polls List)

| UI ID | Type | Data Source | Shape | Notes |
|-------|------|-------------|-------|-------|
| UI:TW:02.1:01 | Table Polls | Merged | [{pollId,question,status,voters,engagementRate}] | Sortable |
| UI:TW:02.1:02 | Status Filter | Local | {status[]} | Multi-select |
| UI:TW:02.1:03 | Create Button | N/A | {} | Permission: Admin/Moderator |
| UI:TW:02.1:04 | Row Menu | Row | {actions[]} | Close / Duplicate |

### Actions & Events (Polls List)

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:02.1:01 | Create Poll | EVT:poll.created | WF:poll-create |
| ACT:TW:02.1:02 | Close Poll | EVT:poll.closed | WF:poll-close |
| ACT:TW:02.1:03 | Duplicate Poll | EVT:poll.duplicated | WF:poll-duplicate |

 
## Page Detail: Poll Create

Page ID: PG:TW:02.2
Purpose: Author & dispatch a new poll.

### UI Elements (Poll Create)

| UI ID | Type | Data | Shape | Notes |
|-------|------|------|-------|-------|
| UI:TW:02.2:01 | Question Input | Form state | {text} | 10..240 chars |
| UI:TW:02.2:02 | Options List | Form state | [{optionId,text}] | 1..10 unique |
| UI:TW:02.2:03 | Platform Selector | Connectors | [{platform,enabled}] | Based on active connectors |
| UI:TW:02.2:04 | Submit Button | Form state | {disabled:boolean} | Disabled until valid |

### Validation Rules (Poll Create)

| Rule | Enforcement |
|------|------------|
| Unique options (case-insensitive) | Server + client |
| Max active polls (20) | Server (config) |

### Actions & Events (Poll Create)

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:02.2:01 | Submit Poll | EVT:poll.created | WF:poll-create |

### Sample Aggregate JSON (Poll)

```json
{
  "pollId": "poll_987",
  "question": "Which topic next week?",
  "status": "active",
  "options": [
    {"optionId": "o1", "text": "Security"},
    {"optionId": "o2", "text": "RAG"}
  ],
  "platformTargets": ["slack","discord"],
  "createdAt": "2025-09-28T15:32:10Z"
}
```

 
## Page Detail: Guardian Violations Queue

Page ID: PG:TW:03.1
Purpose: Prioritize and resolve safety violations.

### Data Sources (Violations Queue)

| Source | Type | Fields | Notes |
|--------|------|--------|-------|
| GQL:GetViolations | GraphQL | violationId, severity, ruleId, status, createdAt | Paginated |
| SQLV:vw_violation_metrics | View | violation_id, time_to_resolve_ms | Post-resolution metrics |
| GQL:GetMessageContext | GraphQL | message{id,content,platform,senderHash} | On select |

### UI Elements (Violations Queue)

| UI ID | Type | Data | Shape | Notes |
|-------|------|------|-------|-------|
| UI:TW:03.1:01 | Table Violations | GQL:GetViolations | [{violationId,severity,status,age}] | Severity color code |
| UI:TW:03.1:02 | Detail Drawer | Combined | {violation, message, actions[]} | Lazy load context |
| UI:TW:03.1:03 | Filter Bar | Local | {severity[],status[]} | Persist in URL |

### Actions & Events (Violations Queue)

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:03.1:01 | Resolve (Dismiss) | EVT:guardian.flag.resolved | WF:violation-resolve |
| ACT:TW:03.1:02 | Resolve (Actioned) | EVT:guardian.flag.resolved | WF:violation-resolve |
| ACT:TW:03.1:03 | Suggest Rule | EVT:guardian.rule.suggested | WF:rule-suggest |

### Sample Aggregate JSON (GuardianViolation)

```json
{
  "violationId": "v_445",
  "messageId": "m_991",
  "severity": "high",
  "ruleId": "rule_profanity_en",
  "status": "open",
  "createdAt": "2025-09-28T12:00:00Z"
}
```

 
## Additional Page Index (Abbreviated)

| Page ID | Name | Summary |
|---------|------|---------|
| PG:TW:01.1 | Dashboard | Multi-metric overview |
| PG:TW:04.1 | Connectors | Manage connectors & auth |
| PG:TW:05.1 | Ask‑DB Ask | Query knowledge |
| PG:TW:05.2 | Ask‑DB Sources | Manage sources |
| PG:TW:06.1 | Broadcast Composer | Create broadcast |
| PG:TW:06.2 | Broadcast History | Delivery results |
| PG:TW:07.1 | Engagement Analytics | Engagement charts |
| PG:TW:07.2 | Safety Analytics | Safety metrics |
| PG:TW:07.3 | Custom Reports | Custom query builder |
| PG:TW:08.1 | Users | Manage user roles |
| PG:TW:09.1 | Billing | Plan & invoices |
| PG:TW:10.1 | Settings | Retention & privacy |
| PG:TW:11.1 | Audit Logs | Searchable audit list |
| PG:TW:12.1 | Data Exports | Request/download exports |

---
Navigation: [Home](home.md) | Previous: [Global Admin Portal Sitemap](portal_global_admin_sitemap.md) | Next: [RBAC Matrix](rbac_matrix.md)
