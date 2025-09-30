# Sequence Diagrams (Key Flows)

```mermaid
sequenceDiagram
    participant Platform
    participant Webhook
    participant IngestWorker
    participant DB
    participant Guardian
    Platform->>Webhook: Message
    Webhook->>IngestWorker: Normalized payload
    IngestWorker->>DB: Persist message
    IngestWorker-->>Guardian: message.ingested event
```

Additional flows: poll vote, guardian flag, Ask‑DB answer, broadcast dispatch.

---
Navigation: [Home](home.md) | Previous: [Architecture Overview](architecture_overview.md) | Next: [Class & Aggregate Diagrams](class_aggregate_diagrams.md)
