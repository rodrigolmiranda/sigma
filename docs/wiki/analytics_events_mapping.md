# Analytics Events Mapping

| Source Event | Transformation | Target Table.Field |
|--------------|---------------|--------------------|
| message.ingested | Count per tenant/day | metrics_daily.messages |
| poll.vote.recorded | Unique voter aggregation | poll_metrics.engagement_rate |
| guardian.flag.raised + guardian.flag.resolved | Duration calculation | guardian_metrics.triage_time_ms |
| askdb.answer.produced | Latency stats | rag_metrics.latency_ms |

---
Navigation: [Home](home.md) | Previous: [Metrics Schema](metrics_schema.md) | Next: [Dashboards Spec](dashboards_spec.md)
