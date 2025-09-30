# Playbook: Polling Module

| Step | Action | Validation |
|------|--------|-----------|
| 1 | Create poll via GraphQL mutation | Response includes pollId |
| 2 | Vote endpoints exercised | Tally updates; idempotent re-vote |
| 3 | Close poll | Status set; final engagement calc |

---
Navigation: [Home](home.md) | Previous: [Connectors Playbook](playbook_connectors.md) | Next: [Guardian Module Playbook](playbook_guardian_module.md)
