# API Schema Evolution

Principles: Backward compatibility, additive changes, deprecation warnings.

## Change Process

1. Propose field / type change via RFC.
2. Add new fields (never repurpose existing).
3. Mark deprecated (GraphQL @deprecated) with timeline.
4. Remove after >= 2 minor versions & zero usage.

---
Navigation: [Home](home.md) | Previous: [Caching Strategy](caching_strategy.md) | Next: [Security & Privacy Controls](sec_privacy_controls.md)
