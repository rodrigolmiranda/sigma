# Tenant Workspace Page: Ask-DB Ask

| Field | Value |
|-------|-------|
| Spec Version | 0.1.0 |
| Status | Draft |
| Last Updated | 2025-09-28 |
| Owner | Knowledge Team |
| Page ID | PG:TW:05.1 |

## Purpose

Natural language query interface over tenant knowledge base.

## Data Sources

| Source | Type | Fields | Class | PII | Notes |
|--------|------|--------|-------|-----|-------|
| API:ragQuery | Service | question, answer, sources[] | Confidential | N | Streaming |

## UI Elements

| UI ID | Type | Data | Shape | Class | PII | Notes |
|-------|------|------|-------|-------|-----|-------|
| UI:TW:05.1:01 | Question Input | form | {text} | Confidential | N | 512 char max |
| UI:TW:05.1:02 | Answer Panel | ragQuery | {answer,sources[]} | Confidential | N | Source chips |

## Actions & Events

| Action ID | Action | Event | Workflow |
|-----------|--------|-------|----------|
| ACT:TW:05.1:01 | Submit Question | EVT:ask.query.submitted | WF:ask-query |

---
Navigation: [Connectors](tw_pg_04_1_connectors.md) | Next: [Ask Sources](tw_pg_05_2_ask_sources.md)
