# Impact Matrix

Purpose: Traceability across pages → actions → events → aggregates → metrics → (future) tests. Used for impact analysis & automated dependency graph.

| Page ID | Action IDs (subset) | Events Emitted | Aggregates Touched | Metrics Affected | Test Hooks (planned) |
|---------|--------------------|----------------|--------------------|------------------|----------------------|
| PG:GA:01.1 | ACT:GA:01.1:01..04 | EVT:tenant.created / suspended / resumed | Tenant, Subscription | MET:ten:tenant_count_active, MET:fin:mrr | TEST:ga.tenants.create.basic |
| PG:GA:01.2 | ACT:GA:01.2:01..04 | EVT:tenant.plan.changed / addon.enabled / limit.adjusted | Tenant, Subscription | MET:fin:mrr, MET:fin:ltv | TEST:ga.tenant.plan.change |
| PG:GA:02.1 | ACT:GA:02.1:01..03 | EVT:plan.created / updated / archived | Plan | MET:fin:mrr | TEST:ga.plan.lifecycle |
| PG:GA:02.2 | ACT:GA:02.2:01 | EVT:invoice.export.created | Invoice | MET:fin:arr | TEST:ga.invoice.export |
| PG:GA:03.1 | ACT:GA:03.1:01 | EVT:finance.export.created | Subscription, Invoice | MET:fin:mrr, MET:fin:arr, MET:fin:churn_rate | TEST:ga.revenue.export.csv |
| PG:GA:04.1 | ACT:GA:04.1:01..03 | EVT:usage.threshold.alerted / usage.report.exported | UsageRecord, AddOnEntitlement | MET:usage:capacity_utilization, MET:usage:overage_events | TEST:ga.usage.alert.threshold |
| PG:GA:05.1 | ACT:GA:05.1:01 | EVT:health.snapshot.exported | IngestStat, QueueStat | MET:ops:ingest_latency_p95 | TEST:ga.health.snapshot |
| PG:GA:06.1 | ACT:GA:06.1:01..02 | EVT:compliance.report.exported / retention.policy.changed | RetentionPolicy, AuditEvent | MET:sec:retention_coverage, MET:sec:audit_volume_24h | TEST:ga.compliance.policy.change |
| PG:GA:07.1 | ACT:GA:07.1:01..04 | EVT:flag.created / flag.updated / flag.archived / rollout.launched | FeatureFlag | MET:flags:active_count, MET:flags:rollout_success_rate | TEST:ga.flags.lifecycle |
| PG:GA:08.1 | ACT:GA:08.1:01..03 | EVT:connector.version.approved / connector.deprecated / connector.health.revalidated | ConnectorDefinition | MET:connector:active, MET:connector:deprecated | TEST:ga.connectors.registry.update |
| PG:GA:09.1 | ACT:GA:09.1:01..03 | EVT:internal.user.invited / internal.user.role.changed / internal.user.deactivated | InternalUser | MET:internal:user_active_count | TEST:ga.internal.user.invite |
| PG:GA:10.1 | ACT:GA:10.1:01..04 | EVT:incident.created / incident.severity.updated / incident.resolved / incident.postmortem.published | Incident | MET:incident:open_count, MET:incident:mttr | TEST:ga.incident.lifecycle |
| PG:GA:11.1 | ACT:GA:11.1:01 | EVT:global.config.saved | GlobalConfig | MET:config:version_height | TEST:ga.global.config.save |
| PG:TW:01.1 | ACT:TW:01.1:01..02 | EVT:dashboard.widget.customized / dashboard.widget.reset | DashboardPreference | MET:eng:dashboard_custom_rate | TEST:tw.dashboard.customize |
| PG:TW:02.1 | ACT:TW:02.1:01..03 | EVT:poll.created / poll.closed / poll.duplicated | Poll, PollVote | MET:poll:engagement_rate | TEST:tw.polls.create.list |
| PG:TW:02.2 | ACT:TW:02.2:01 | EVT:poll.created | Poll | MET:poll:engagement_rate | TEST:tw.polls.create.validate |
| PG:TW:02.3 | ACT:TW:02.3:01..02 | EVT:poll.analytics.segment.exported / poll.analytics.drill | PollAnalytics | MET:poll:segmentation_depth | TEST:tw.poll.analytics.export |
| PG:TW:03.1 | ACT:TW:03.1:01..03 | EVT:guardian.flag.resolved / guardian.rule.suggested | GuardianViolation, Rule | MET:guardian:triage_time_mean | TEST:tw.guardian.resolve.violation |
| PG:TW:04.1 | ACT:TW:04.1:01..02 | EVT:connector.instance.added / connector.instance.removed | ConnectorInstance | MET:connector:tenant_active | TEST:tw.connectors.add.remove |
| PG:TW:05.1 | ACT:TW:05.1:01..03 | EVT:ask.query.executed / ask.answer.voted / ask.answer.bookmarked | AskQuery, AskAnswer | MET:ask:query_latency_p95, MET:ask:engagement_rate | TEST:tw.ask.query.latency |
| PG:TW:05.2 | ACT:TW:05.2:01..03 | EVT:ask.source.added / ask.source.reindexed / ask.source.removed | KnowledgeSource | MET:ask:sources_active | TEST:tw.ask.sources.manage |
| PG:TW:06.1 | ACT:TW:06.1:01 | EVT:broadcast.created | Broadcast | MET:broadcast:reach | TEST:tw.broadcast.compose |
| PG:TW:06.2 | ACT:TW:06.2:01..02 | EVT:broadcast.retry.requested / broadcast.metrics.exported | Broadcast, DeliveryResult | MET:broadcast:delivery_success_rate | TEST:tw.broadcast.history.retry |
| PG:TW:07.1 | ACT:TW:07.1:01..02 | EVT:analytics.engagement.segment.exported / analytics.engagement.chart.drill | MetricsDaily | MET:eng:active_users_7d | TEST:tw.analytics.engagement.export |
| PG:TW:07.2 | ACT:TW:07.2:01..02 | EVT:analytics.safety.segment.exported / analytics.safety.chart.drill | MetricsDaily, GuardianViolation | MET:guardian:violations_rate | TEST:tw.analytics.safety.export |
| PG:TW:07.3 | ACT:TW:07.3:01..03 | EVT:analytics.custom.report.created / analytics.custom.report.run / analytics.custom.report.deleted | CustomReport | MET:analytics:custom_reports_active | TEST:tw.analytics.custom.report.lifecycle |
| PG:TW:08.1 | ACT:TW:08.1:01..03 | EVT:user.invited / user.role.changed / user.deactivated | User | MET:user:active_count | TEST:tw.users.invite |
| PG:TW:09.1 | ACT:TW:09.1:01..02 | EVT:tenant.subscription.updated / invoice.downloaded | Subscription, Invoice | MET:fin:mrr | TEST:tw.billing.plan.update |
| PG:TW:10.1 | ACT:TW:10.1:01..02 | EVT:retention.policy.updated / retention.export.requested | RetentionPolicy | MET:sec:retention_coverage | TEST:tw.settings.retention.update |
| PG:TW:11.1 | ACT:TW:11.1:01 | EVT:audit.logs.exported | AuditEvent | MET:audit:events_7d | TEST:tw.audit.export |
| PG:TW:12.1 | ACT:TW:12.1:01..02 | EVT:export.requested / export.downloaded | ExportJob | MET:export:jobs_active | TEST:tw.export.lifecycle |

Legend: compressed ranges ACT:GA:01.1:01..04 expand numerically.

Pending Schema Additions:

- Add any new MET:* IDs above to `metrics_schema.md` (placeholders added for usage, security, flags, connector, ask modules).
- Add new EVT:* definitions to `event_catalog.md` with domain, version, and payload contract.

Validation Roadmap:

1. ID Integrity: Script will cross-check every Page ID from `id_namespace.json` has a row here.
2. Event Coverage: Each Action row must have Event OR explicit (none) marker.
3. Metric Referencing: All MET:* tokens must exist in `metrics_schema.md`.
4. Test Hook Stubs: TEST:* names become keys in future automated test registry.


Update Workflow: When adding a new page spec, append row; if events new, update `event_catalog.md`; ensure metrics appear in `metrics_schema.md`.

---
Navigation: [Home](home.md) | Previous: [Naming Conventions](naming_conventions.md) | Next: [Global Admin Portal Sitemap](portal_global_admin_sitemap.md)
