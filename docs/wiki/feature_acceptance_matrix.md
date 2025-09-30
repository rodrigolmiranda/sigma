# Feature Acceptance Matrix

## Purpose

Trace feature scope → acceptance criteria → metrics.

| Feature | Criteria (Excerpt) | Events Emitted | Metrics Impacted | Phase |
|---------|--------------------|----------------|------------------|-------|
| Poll Creation | All validation rules pass; poll persisted; platforms targeted subset valid | `poll.created` | Poll Engagement Rate | 1 |
| Poll Vote | Vote idempotent; count updated; tally consistent | `poll.vote.recorded` | Poll Engagement Rate | 1 |
| Guardian Flag | Rule triggered; violation record stored; severity assigned | `guardian.flag.raised` | Triage Time | 1 |
| Flag Resolve | Resolution reason captured; status change audit | `guardian.flag.resolved` | Triage Time | 1 |
| Ask‑DB Question | Embedding dedup; retrieval executed; answer scored | `askdb.question.asked` / `askdb.answer.produced` | RAG Adoption | 2 |
| Broadcast Dispatch | Message fan-out initiated; per-platform results tracked | `broadcast.dispatched` | Outreach Reach | 2 |

---
Navigation: [Home](home.md) | Previous: [PRD Master](prd_master.md) | Next: [Pricing Model](pricing_model.md)
