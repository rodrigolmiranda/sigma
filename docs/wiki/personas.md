# Personas

## Overview

Personas guide requirement priority, UX tone, and RBAC defaults. Empathy mapping deepens domain event design.

## Persona Cards

### Community Manager (Primary)

* Goals: Healthy engagement, quick insight into participation.
* Pain: Fragmented platform metrics, manual poll coordination.
* Success: Higher poll participation & reduced moderation overhead.
* Key Features: Dashboard, Polls, Guardian Queue (overview), Broadcasts.
* RBAC: Admin or Owner.

### Moderator

* Goals: Rapid flag triage, consistent policy enforcement.
* Pain: Noise vs real violations, context switching.
* Success: High triage throughput, low false positives.
* Key Features: Guardian Queue, Rule Suggestions, Limited Analytics.
* RBAC: Moderator.

### Analyst

* Goals: Extract actionable metrics & trend deltas.
* Pain: Raw exports; manual reconciliation.
* Success: Clear comparisons, segment breakdowns.
* Key Features: Analytics Dashboards, Metrics Exports.
* RBAC: Analyst.

### Educator / Facilitator

* Goals: Gauge learning cohort interaction without LMS overhead.
* Pain: Hard to measure engagement across multiple channels.
* Success: Poll participation & sentiment signals.
* Key Features: Poll Creation, Engagement Dashboard.
* RBAC: Admin or Moderator (limited).

### Enterprise IT (Secondary - Phase 3)

* Goals: Compliance, security posture, integration oversight.
* Key Features: Audit Logs, Role Management, Retention Settings.
* RBAC: Owner.

## Persona ↔ Feature Mapping

| Feature | Primary Persona | Secondary | Notes |
|---------|-----------------|----------|-------|
| Poll Create | Community Manager | Educator | High frequency action |
| Guardian Queue | Moderator | Community Manager | Moderator is operational owner |
| Ask‑DB Query | Community Manager | Analyst | Analyst leverages insights |
| Broadcast Dispatch | Community Manager | Moderator | Moderator may review copy |

---
Navigation: [Home](home.md) | Previous: [Strategy & Phasing](strategy_phasing.md) | Next: [Jobs To Be Done](jobs_to_be_done.md)
