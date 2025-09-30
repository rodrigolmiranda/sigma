# Tenant Workspace Page: Broadcast History

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Messaging Team |
| Page ID | PG:TW:06.2 |

## Purpose

Review broadcast delivery results and status.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetBroadcasts | GraphQL | broadcastId, createdAt, status | Confidential | N | Paginated |
| SQLV:vw_broadcast_delivery | View | broadcast_id, platform, delivered, failed | Confidential | N | Aggregated |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:06.2:01 | Broadcasts Table | broadcasts | [{broadcastId,createdAt,status}] | Confidential | N | Filter by status |
| UI:TW:06.2:02 | Delivery Drawer | delivery view | {platforms[]} | Confidential | N | Drilldown |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:06.2:01 | Export Results | EVT:broadcast.export.created | WF:broadcast-export |

---
Navigation: [Broadcast Composer](tw_pg_06_1_broadcast_composer.md) | Next: [Engagement Analytics](tw_pg_07_1_engagement_analytics.md)
