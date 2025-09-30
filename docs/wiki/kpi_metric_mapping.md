# KPI ↔ Metric Mapping

| KPI | Definition | Source Events | Derived Table/Field |
|-----|------------|---------------|---------------------|
| Poll Engagement Rate | Unique voters / eligible members | `poll.vote.recorded` + roster snapshot | `poll_metrics.engagement_rate` |
| Mean Flag Triage Time | Time between raised & resolved | `guardian.flag.raised` + `guardian.flag.resolved` | `guardian_metrics.triage_time_mean` |
| RAG Adoption | Tenants using Ask‑DB / active tenants | `askdb.question.asked` | `tenant_metrics.rag_adoption` |
| Broadcast Reach | Delivered platform messages | `broadcast.platform.delivery.succeeded` | `broadcast_metrics.reach` |

---
Navigation: [Home](home.md) | Previous: [Pricing Model](pricing_model.md) | Next: [Competitor Playbook](competitor_playbook.md)
