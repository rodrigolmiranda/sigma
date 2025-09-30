# Page Contract: Dashboard

| Field/Widget | Source | Update Interval | Role Visibility | Notes |
|--------------|--------|----------------|-----------------|-------|
| Active Polls Count | polls table | Real-time | All (Viewer limited) | Limit viewer detail |
| Open Flags | guardian_violations | 1m | Moderator+ | Redaction for non-privileged |
| Engagement Trend | metrics_daily | 1h | Admin+/Analyst | Sparkline |

## Actions

None (navigational hub).

## Events Referenced

`poll.created`, `poll.vote.recorded`, `guardian.flag.raised`.

---
Navigation: [Home](home.md) | Previous: [RBAC Matrix](rbac_matrix.md) | Next: [Poll Create Page Contract](page_contract_poll_create.md)
