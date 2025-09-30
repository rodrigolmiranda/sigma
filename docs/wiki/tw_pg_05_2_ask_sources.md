# Tenant Workspace Page: Ask-DB Sources

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Knowledge Team |
| Page ID | PG:TW:05.2 |

## Purpose

Manage knowledge source documents and embedding status.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| GQL:GetKnowledgeSources | GraphQL | sourceId, name, status, sizeBytes | Confidential | N | |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:05.2:01 | Sources Table | sources | [{sourceId,name,status,sizeBytes}] | Confidential | N | Status badges |
| UI:TW:05.2:02 | Upload Button | N/A | {} | Internal | N | Async upload |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:05.2:01 | Upload Source | EVT:ask.source.uploaded | WF:ask-source-upload |
| ACT:TW:05.2:02 | Re-Embed Source | EVT:ask.source.reembed | WF:ask-source-embed |

---
Navigation: [Ask](tw_pg_05_1_ask.md) | Next: [Broadcast Composer](tw_pg_06_1_broadcast_composer.md)
