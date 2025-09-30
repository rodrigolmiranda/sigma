# Page Contract: Poll Create

Purpose: Create multi-platform poll.

## UI Components

* Question input (required 10..240 chars)
* Options list (1..10 unique)
* Platform selector
* Submit (disabled until valid)

## Validation

| Rule | Enforcement |
|------|-------------|
| Unique options case-insensitive | Server + client |
| Max active polls / tenant (20) | Server (config) |

## Events

Emits `poll.created`.

---
Navigation: [Home](home.md) | Previous: [Dashboard Page Contract](page_contract_dashboard.md) | Next: [Guardian Queue Page Contract](page_contract_guardian_queue.md)
